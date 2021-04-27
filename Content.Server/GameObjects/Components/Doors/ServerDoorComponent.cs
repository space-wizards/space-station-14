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
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;
using Content.Server.GameObjects.Components.Construction;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedDoorComponent))]
    public class ServerDoorComponent : SharedDoorComponent, IActivate, IStartCollide, IInteractUsing, IMapInit
    {
        [ComponentDependency]
        private readonly IDoorCheck? _doorCheck = null;

        [ViewVariables]
        [DataField("board")]
        private string? _boardPrototype;

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

        private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5);

        private CancellationTokenSource? _stateChangeCancelTokenSource;
        private CancellationTokenSource? _autoCloseCancelTokenSource;

        private const int DoorCrushDamage = 15;
        private const float DoorStunTime = 5f;

        /// <summary>
        /// Whether the door will ever crush.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("inhibitCrush")]
        private bool _inhibitCrush;

        /// <summary>
        /// Whether the door blocks light.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("occludes")]
        private bool _occludes = true;
        public bool Occludes => _occludes;

        /// <summary>
        /// Whether the door will open when it is bumped into.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("bumpOpen")]
        private bool _bumpOpen = true;

        /// <summary>
        /// Whether the door starts open when it's first loaded from prototype. A door won't start open if its prototype is also welded shut.
        /// Handled in Startup().
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("startOpen")]
        private bool _startOpen;

        /// <summary>
        /// Whether the airlock is welded shut. Can be set by the prototype, although this will fail if the door isn't weldable.
        /// When set by prototype, handled in Startup().
        /// </summary>
        [DataField("welded")]
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
        [DataField("weldable")]
        private bool _weldable = true;

        /// <summary>
        /// Whether the door can currently be welded.
        /// </summary>
        private bool CanWeldShut => _weldable && State == DoorState.Closed;

        /// <summary>
        ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
        /// </summary>
        private bool _beingWelded;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canCrush")]
        private bool _canCrush = true;

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

            CreateDoorElectronicsBoard();
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

            CreateDoorElectronicsBoard();
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

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
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

                TryOpen(otherFixture.Body.Owner);
            
        }

        #region Opening

        public void TryOpen(IEntity user)
        {
            if (CanOpenByEntity(user))
            {
                Open();

                if (user.TryGetComponent(out HandsComponent? hands) && hands.Count == 0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/bang.ogg", Owner,
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

            if (safety && Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                var broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();

                // Use this version so we can ignore the CanCollide being false
                foreach(var e in broadPhaseSystem.GetCollidingEntities(physicsComponent.Owner.Transform.MapID, physicsComponent.GetWorldAABB()))
                {
                    if ((physicsComponent.CollisionMask & e.CollisionLayer) != 0 && broadPhaseSystem.IntersectionPercent(physicsComponent, e) > 0.01f) return true;
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

            var doorAABB = PhysicsComponent.GetWorldAABB();
            var hitsomebody = false;

            // Crush
            foreach (var e in collidingentities)
            {
                if (!e.Owner.TryGetComponent(out StunnableComponent? stun)
                    || !e.Owner.TryGetComponent(out IDamageableComponent? damage))
                {
                    continue;
                }

                var percentage = e.GetWorldAABB().IntersectPercentage(doorAABB);

                if (percentage < 0.1f)
                    continue;

                hitsomebody = true;
                CurrentlyCrushing.Add(e.Owner.Uid);

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

            Owner.SpawnRepeatingTimer(realCloseTime, async () =>
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
                        // just in case
                        if (!CanWeldShut)
                        {
                            return false;
                        }

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

        /// <summary>
        ///     Creates the corresponding door electronics board on the door.
        ///     This exists so when you deconstruct doors that were serialized with the map,
        ///     you can retrieve the door electronics board.
        /// </summary>
        private void CreateDoorElectronicsBoard()
        {
            // Ensure that the construction component is aware of the board container.
            if (Owner.TryGetComponent(out ConstructionComponent? construction))
                construction.AddContainer("board");

            // We don't do anything if this is null or empty.
            if (string.IsNullOrEmpty(_boardPrototype))
                return;

            var container = Owner.EnsureContainer<Container>("board", out var existed);

            return;
            /* // TODO ShadowCommander: Re-enable when access is added to boards. Requires map update.
            if (existed)
            {
                // We already contain a board. Note: We don't check if it's the right one!
                if (container.ContainedEntities.Count != 0)
                    return;
            }

            var board = Owner.EntityManager.SpawnEntity(_boardPrototype, Owner.Transform.Coordinates);

            if(!container.Insert(board))
                Logger.Warning($"Couldn't insert board {board} into door {Owner}!");
            */
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new DoorComponentState(State, StateChangeStartTime, CurrentlyCrushing, GameTiming.CurTime);
        }
    }
}
