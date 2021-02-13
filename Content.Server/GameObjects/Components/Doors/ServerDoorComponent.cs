#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Doors;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Doors;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedDoorComponent))]
    public class ServerDoorComponent : SharedDoorComponent, IActivate, ICollideBehavior, IInteractUsing, IMapInit
    {
        [ComponentDependency]
        private readonly IDoorCheck? _doorCheck = null;
        
        public override DoorState State
        {
            get => base.State;
            protected set
            {
                if (State == value)
                {
                    return;
                }

                base.State = value;

                StateChangeStartTime = State switch
                {
                    DoorState.Open or DoorState.Closed => null,
                    DoorState.Opening or DoorState.Closing => GameTiming.CurTime,
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (_doorCheck != null)
                {
                    _doorCheck.OnStateChange(State);
                    RefreshAutoClose();
                }

                Dirty();
            }
        }

        /// <summary>
        /// The amount of time the door has been open. Used to automatically close the door if it autocloses.
        /// </summary>
        [ViewVariables]
        private float _openTimeCounter;
        [ViewVariables(VVAccess.ReadWrite)]

        private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5);

        private CancellationTokenSource? _stateChangeCancelTokenSource;
        private CancellationTokenSource? _autoCloseCancelTokenSource;

        private const int DoorCrushDamage = 15;
        private const float DoorStunTime = 5f;

        /// <summary>
        /// Whether the door will ever crush.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        private bool _inhibitCrush;

        /// <summary>
        /// Whether the door blocks light.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] private bool _occludes;
        public bool Occludes => _occludes;

        /// <summary>
        /// Whether the door will open when it is bumped into.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] private bool _bumpOpen;

        /// <summary>
        /// Whether the door starts open when it's first loaded from prototype. A door won't start open if its prototype is also welded shut.
        /// Handled in Startup().
        /// </summary>
        private bool _startOpen;

        /// <summary>
        /// Whether the airlock is welded shut. Can be set by the prototype, although this will fail if the door isn't weldable.
        /// When set by prototype, handled in Startup().
        /// </summary>
        private bool _isWeldedShut;
        /// <summary>
        /// Whether the airlock is welded shut.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                if (_isWeldedShut == value)
                {
                    return;
                }

                _isWeldedShut = value;
                SetAppearance(_isWeldedShut ? DoorVisualState.Welded : DoorVisualState.Closed);
            }
        }

        /// <summary>
        /// Whether the door can ever be welded shut.
        /// </summary>
        private bool _weldable;
        /// <summary>
        /// Whether the door can currently be welded.
        /// </summary>
        private bool CanWeldShut => _weldable && State == DoorState.Closed;

        /// <summary>
        ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
        /// </summary>
        private bool _beingWelded = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _isWeldedShut, "welded", false);
            serializer.DataField(ref _startOpen, "startOpen", false);
            serializer.DataField(ref _weldable, "weldable", true);
            serializer.DataField(ref _bumpOpen, "bumpOpen", true);
            serializer.DataField(ref _occludes, "occludes", true);
            serializer.DataField(ref _inhibitCrush, "inhibitCrush", false);
        }

        protected override void Startup()
        {
            base.Startup();

            if (IsWeldedShut)
            {
                if (!CanWeldShut)
                {
                    Logger.Warning("{0} prototype loaded with incompatible flags: 'welded' is true, but door cannot be welded.", Owner.Name);
                    return;
                }
                SetAppearance(DoorVisualState.Welded);
            }
        }

        public override void OnRemove()
        {
            _stateChangeCancelTokenSource?.Cancel();
            _autoCloseCancelTokenSource?.Cancel();

            base.OnRemove();
        }

        void IMapInit.MapInit()
        {
            if (_startOpen)
            {
                if (IsWeldedShut)
                {
                    Logger.Warning("{0} prototype loaded with incompatible flags: 'welded' and 'startOpen' are both true.", Owner.Name);
                    return;
                }
                QuickOpen();
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_doorCheck != null && _doorCheck.BlockActivate(eventArgs))
            {
                return;
            }

            if (State == DoorState.Open)
            {
                TryClose(eventArgs.User);
            }
            else if (State == DoorState.Closed)
            {
                TryOpen(eventArgs.User);
            }
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (State != DoorState.Closed)
            {
                return;
            }

            if (!_bumpOpen)
            {
                return;
            }

            // Disabled because it makes it suck hard to walk through double doors.

            if (entity.HasComponent<IBody>())
            {
                if (!entity.TryGetComponent<IMoverComponent>(out var mover)) return;

                /*
                // TODO: temporary hack to fix the physics system raising collision events akwardly.
                // E.g. when moving parallel to a door by going off the side of a wall.
                var (walking, sprinting) = mover.VelocityDir;
                // Also TODO: walking and sprint dir are added together here
                // instead of calculating their contribution correctly.
                var dotProduct = Vector2.Dot((sprinting + walking).Normalized, (entity.Transform.WorldPosition - Owner.Transform.WorldPosition).Normalized);
                if (dotProduct <= -0.85f)
                    TryOpen(entity);
                */

                TryOpen(entity);
            }
        }

        #region Opening

        public void TryOpen(IEntity user)
        {
            if (CanOpenByEntity(user))
            {
                Open();

                if (user.TryGetComponent(out HandsComponent? hands) && hands.Count == 0)
                {
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/bang.ogg", Owner,
                                                                   AudioParams.Default.WithVolume(-2));
                }
            }
            else
            {
                Deny();
            }
        }

        public bool CanOpenByEntity(IEntity user)
        {
            if(!CanOpenGeneric())
            {
                return false;
            }

            if (!Owner.TryGetComponent(out AccessReader? access))
            {
                return true;
            }

            var doorSystem = EntitySystem.Get<ServerDoorSystem>();
            var isAirlockExternal = HasAccessType("External");

            return doorSystem.AccessType switch
            {
                ServerDoorSystem.AccessTypes.AllowAll => true,
                ServerDoorSystem.AccessTypes.AllowAllIdExternal => isAirlockExternal || access.IsAllowed(user),
                ServerDoorSystem.AccessTypes.AllowAllNoExternal => !isAirlockExternal,
                _ => access.IsAllowed(user)
            };
        }

        /// <summary>
        /// Returns whether a door has a certain access type. For example, maintenance doors will have access type
        /// "Maintenance" in their AccessReader.
        /// </summary>
        private bool HasAccessType(string accessType)
        {
            if (Owner.TryGetComponent(out AccessReader? access))
            {
                return access.AccessLists.Any(list => list.Contains(accessType));
            }

            return true;
        }

        /// <summary>
        /// Checks if we can open at all, for anyone or anything. Will return false if inhibited by an IDoorCheck component.
        /// </summary>
        /// <returns>Boolean describing whether this door can open.</returns>
        public bool CanOpenGeneric()
        {
            // note the welded check -- CanCloseGeneric does not have this
            if (IsWeldedShut)
            {
                return false;
            }
            if(_doorCheck != null)
            {
                return _doorCheck.OpenCheck();
            }
            
            return true;
        }

        /// <summary>
        /// Opens the door. Does not check if this is possible.
        /// </summary>
        public void Open()
        {
            State = DoorState.Opening;
            if (Occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }

            _stateChangeCancelTokenSource?.Cancel();
            _stateChangeCancelTokenSource = new();

            Owner.SpawnTimer(OpenTimeOne, async () =>
            {
                OnPartialOpen();
                await Timer.Delay(OpenTimeTwo, _stateChangeCancelTokenSource.Token);

                State = DoorState.Open;
            }, _stateChangeCancelTokenSource.Token);
        }

        protected override void OnPartialOpen()
        {
            if (Owner.TryGetComponent(out AirtightComponent? airtight))
            {
                airtight.AirBlocked = false;
            }
            base.OnPartialOpen();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, false));
        }

        private void QuickOpen()
        {
            if (Occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }
            OnPartialOpen();
            State = DoorState.Open;
        }

        #endregion

        #region Closing

        public void TryClose(IEntity user)
        {
            if (!CanCloseByEntity(user))
            {
                Deny();
                return;
            }

            Close();
        }

        public bool CanCloseByEntity(IEntity user)
        {
            if (!CanCloseGeneric())
            {
                return false;
            }

            if (!Owner.TryGetComponent(out AccessReader? access))
            {
                return true;
            }

            return access.IsAllowed(user);
        }

        /// <summary>
        /// Checks if we can close at all, for anyone or anything. Will return false if inhibited by an IDoorCheck component or if we are colliding with somebody while our Safety is on.
        /// </summary>
        /// <returns>Boolean describing whether this door can close.</returns>
        public bool CanCloseGeneric()
        {
            if (_doorCheck != null && !_doorCheck.CloseCheck())
            {
                return false;
            }

            return !IsSafetyColliding();
        }

        private bool SafetyCheck()
        {
            return (_doorCheck != null && _doorCheck.SafetyCheck()) || _inhibitCrush;
        }

        /// <summary>
        /// Checks if we care about safety, and if so, if something is colliding with it; ignores the CanCollide of the door's PhysicsComponent.
        /// </summary>
        /// <returns>True if something is colliding with us and we shouldn't crush things, false otherwise.</returns>
        private bool IsSafetyColliding()
        {
            var safety = SafetyCheck();

            if (safety && PhysicsComponent != null)
            {
                var physics = IoCManager.Resolve<IPhysicsManager>();

                foreach(var e in physics.GetCollidingEntities(Owner.Transform.MapID, PhysicsComponent.WorldAABB))
                {
                    if (e.CanCollide &&
                       ((PhysicsComponent.CollisionMask & e.CollisionLayer) != 0x0 ||
                        (PhysicsComponent.CollisionLayer & e.CollisionMask) != 0x0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Closes the door. Does not check if this is possible.
        /// </summary>
        public void Close()
        {
            State = DoorState.Closing;
            _openTimeCounter = 0;

            // no more autoclose; we ARE closed
            _autoCloseCancelTokenSource?.Cancel();

            _stateChangeCancelTokenSource?.Cancel();
            _stateChangeCancelTokenSource = new();
            Owner.SpawnTimer(CloseTimeOne, async () =>
            {
                // if somebody walked into the door as it was closing, and we don't crush things
                if (IsSafetyColliding())
                {
                    Open();
                    return;
                }

                OnPartialClose();
                await Timer.Delay(CloseTimeTwo, _stateChangeCancelTokenSource.Token);
                
                if (Occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
                {
                    occluder.Enabled = true;
                }

                State = DoorState.Closed;
            }, _stateChangeCancelTokenSource.Token);
        }

        protected override void OnPartialClose()
        {
            base.OnPartialClose();

            // if safety is off, crushes people inside of the door, temporarily turning off collisions with them while doing so.
            var becomeairtight = SafetyCheck() || !TryCrush();

            if (becomeairtight && Owner.TryGetComponent(out AirtightComponent? airtight))
            {
                airtight.AirBlocked = true;
            }

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, true));
        }

        /// <summary>
        /// Crushes everyone colliding with us by more than 10%.
        /// </summary>
        /// <returns>True if we crushed somebody, false if we did not.</returns>
        private bool TryCrush()
        {
            if (PhysicsComponent == null)
            {
                return false;
            }

            var collidingentities = PhysicsComponent.GetCollidingEntities(Vector2.Zero, false);

            if (!collidingentities.Any())
            {
                return false;
            }

            var doorAABB = PhysicsComponent.WorldAABB;
            var hitsomebody = false;

            // Crush
            foreach (var e in collidingentities)
            {
                if (!e.TryGetComponent(out StunnableComponent? stun)
                    || !e.TryGetComponent(out IDamageableComponent? damage)
                    || !e.TryGetComponent(out IPhysicsComponent? otherBody))
                {
                    continue;
                }

                var percentage = otherBody.WorldAABB.IntersectPercentage(doorAABB);

                if (percentage < 0.1f)
                    continue;

                hitsomebody = true;
                CurrentlyCrushing.Add(e.Uid);

                damage.ChangeDamage(DamageType.Blunt, DoorCrushDamage, false, Owner);
                stun.Paralyze(DoorStunTime);
            }

            // If we hit someone, open up after stun (opens right when stun ends)
            if (hitsomebody)
            {
                Owner.SpawnTimer(TimeSpan.FromSeconds(DoorStunTime) - OpenTimeOne - OpenTimeTwo, Open);
                return true;
            }

            return false;
        }

        #endregion

        public void Deny()
        {
            if (_doorCheck != null && !_doorCheck.DenyCheck())
            {
                return;
            }

            if (State == DoorState.Open || IsWeldedShut)
                return;

            _stateChangeCancelTokenSource?.Cancel();
            _stateChangeCancelTokenSource = new();
            SetAppearance(DoorVisualState.Deny);
            Owner.SpawnTimer(DenyTime, () =>
            {
                SetAppearance(DoorVisualState.Closed);
            }, _stateChangeCancelTokenSource.Token);
        }

        /// <summary>
        /// Stops the current auto-close timer if there is one. Starts a new one if this is appropriate (i.e. entity has an IDoorCheck component that allows auto-closing).
        /// </summary>
        public void RefreshAutoClose()
        {
            _autoCloseCancelTokenSource?.Cancel();

            if (State != DoorState.Open || _doorCheck == null || !_doorCheck.AutoCloseCheck())
            {
                return;
            }
            _autoCloseCancelTokenSource = new();

            var realCloseTime = _doorCheck.GetCloseSpeed() ?? AutoCloseDelay;

            Owner.SpawnTimer(realCloseTime, async () =>
            {
                if (CanCloseGeneric())
                {
                    // Close() cancels _autoCloseCancellationTokenSource, so we're fine.
                    Close();
                }
            }, _autoCloseCancelTokenSource.Token);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if(!eventArgs.Using.TryGetComponent(out ToolComponent? tool))
            {
                return false;
            }

            // for prying doors
            if (tool.HasQuality(ToolQuality.Prying) && !IsWeldedShut)
            {
                var successfulPry = false;

                if (_doorCheck != null)
                {
                    _doorCheck.OnStartPry(eventArgs);
                    successfulPry = await tool.UseTool(eventArgs.User, Owner,
                        _doorCheck.GetPryTime() ?? 0.5f, ToolQuality.Prying, () => _doorCheck.CanPryCheck(eventArgs));
                }
                else
                {
                    successfulPry = await tool.UseTool(eventArgs.User, Owner, 0.5f, ToolQuality.Prying);
                }

                if (successfulPry && !IsWeldedShut)
                {
                    if (State == DoorState.Closed)
                    {
                        Open();
                    }
                    else if (State == DoorState.Open)
                    {
                        Close();
                    }
                    return true;
                }
            }

            // for welding doors
            if (CanWeldShut && tool.Owner.TryGetComponent(out WelderComponent? welder) && welder.WelderLit)
            {
                if(!_beingWelded)
                {
                    _beingWelded = true;
                    if(await welder.UseTool(eventArgs.User, Owner, 3f, ToolQuality.Welding, 3f, () => CanWeldShut))
                    {
                        _beingWelded = false;
                        IsWeldedShut = !IsWeldedShut;
                        return true;
                    }
                    _beingWelded = false;
                }
            }
            else
            {
                _beingWelded = false;
            }
            return false;
        }

        public override ComponentState GetComponentState()
        {
            return new DoorComponentState(State, StateChangeStartTime, CurrentlyCrushing, GameTiming.CurTime);
        }
    }
}
