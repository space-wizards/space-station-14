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
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Doors
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ServerDoorComponent : SharedDoorComponent, IActivate, ICollideBehavior, IInteractUsing
    {
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

                if (Owner.TryGetComponent(out IDoorCheck? doorCheck))
                {
                    doorCheck.OnStateChange(State);
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
        public bool AutoClose = true;
        private const float AutoCloseDelay = 5;

        private CancellationTokenSource? _cancellationTokenSource;

        private const int DoorCrushDamage = 15;
        private const float DoorStunTime = 5f;

        [ViewVariables(VVAccess.ReadOnly)]
        private bool _inhibitCrush = false;
        private bool _safety = true;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Safety
        {
            get => (_safety || _inhibitCrush);
            set => _safety = value;
        }

        [ViewVariables(VVAccess.ReadWrite)] private bool _occludes;
        public bool Occludes => _occludes;

        [ViewVariables(VVAccess.ReadWrite)] private bool _bumpOpen;

        public bool BumpOpen => _bumpOpen;

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
        private bool _isWeldedShut;

        private bool _weldable = true;
        private bool CanWeldShut => _weldable && State == DoorState.Closed;

        /// <summary>
        ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
        /// </summary>
        private bool _beingWelded = false;

        /// <summary>
        /// Whether the door starts open when it's first loaded.
        /// </summary>
        private bool _startOpen = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // note -- these two need to be finished up later in Startup(), due to other components not being finished until then.
            // also, an airlock won't start open if its prototype says it's welded shut, or start welded shut if it isn't weldable.
            serializer.DataField(ref _isWeldedShut, "welded", false);
            serializer.DataField(ref _startOpen, "startOpen", false);

            // Whether the door can be welded shut.
            serializer.DataField(ref _weldable, "weldable", true);

            // Whether the door will automatically close if left open.
            serializer.DataField(ref AutoClose, "AutoClose", true);

            // Whether the door will open when it is bumped into.
            serializer.DataField(ref _bumpOpen, "bumpOpen", true);

            // Whether the door blocks light.
            serializer.DataField(ref _occludes, "occludes", true);

            // Whether the door will crush at all. In order to crush, safety AND inhibitCrush must both be false.
            serializer.DataField(ref _inhibitCrush, "inhibitCrush", false);
            // Whether safety is on by default.
            serializer.DataField(ref _safety, "safety", true);
        }

        // necessary to ensure that prototype-loaded welded / open-by-default doors behave correctly
        protected override void Startup()
        {
            base.Startup();

            if (IsWeldedShut && CanWeldShut)
            {
                SetAppearance(DoorVisualState.Welded);
            }
            else if (_startOpen)
            {
                QuickOpen();
            }
        }

        public override void OnRemove()
        {
            _cancellationTokenSource?.Cancel();

            base.OnRemove();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (Owner.TryGetComponent(out IDoorCheck? doorCheck) && doorCheck.BlockActivate(eventArgs))
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

            if (!Owner.TryGetComponent<AccessReader>(out var accessReader))
            {
                return true;
            }

            var doorSystem = EntitySystem.Get<ServerDoorSystem>();
            var isAirlockExternal = HasAccessType("External");

            return doorSystem.AccessType switch
            {
                ServerDoorSystem.AccessTypes.AllowAll => true,
                ServerDoorSystem.AccessTypes.AllowAllIdExternal => isAirlockExternal || accessReader.IsAllowed(user),
                ServerDoorSystem.AccessTypes.AllowAllNoExternal => !isAirlockExternal,
                _ => accessReader.IsAllowed(user)
            };
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
            if(Owner.TryGetComponent(out IDoorCheck? doorCheck) && !doorCheck.OpenCheck())
            {
                return false;
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

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();
            Owner.SpawnTimer(OpenTimeOne, async () =>
            {
                OnPartialOpen();
                await Timer.Delay(OpenTimeTwo, _cancellationTokenSource.Token);

                State = DoorState.Open;
            }, _cancellationTokenSource.Token);
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

        /// <summary>
        /// Returns whether a door has a certain access type. For example, maintenance doors will have access type
        /// "Maintenance" in their AccessReader.
        /// </summary>
        private bool HasAccessType(string accesType)
        {
            if(Owner.TryGetComponent<AccessReader>(out var accessReader))
            {
                return accessReader.AccessLists.Any(list => list.Contains(accesType));
            }

            return true;
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
            if (!Owner.TryGetComponent(out AccessReader? accessReader))
            {
                return true;
            }

            return accessReader.IsAllowed(user);
        }

        /// <summary>
        /// Checks if we can close at all, for anyone or anything. Will return false if inhibited by an IDoorCheck component or if we are colliding with somebody while our Safety is on.
        /// </summary>
        /// <returns>Boolean describing whether this door can close.</returns>
        public bool CanCloseGeneric()
        {
            if (Owner.TryGetComponent(out IDoorCheck? doorCheck) && !doorCheck.CloseCheck())
            {
                return false;
            }

            return SafetyCheck();
        }

        /// <summary>
        /// Checks if we are allowed to crush people, and if something is colliding with the door.
        /// </summary>
        /// <returns>True if we shouldn't worry about something colliding with us, false if we should.</returns>
        public bool SafetyCheck()
        {
            if (Safety && Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                // WE ARE NOT ACTUALLY BECOMING COLLIDEABLE. WE JUST NEED TO SEE IF WE SHOULD DELAY CLOSING DUE TO A PERSON.
                var storedCanCollide = physics.CanCollide;
                physics.CanCollide = true;
                if (physics.IsColliding(Vector2.Zero, false))
                {
                    physics.CanCollide = storedCanCollide;
                    return false;
                }
                physics.CanCollide = storedCanCollide;
            }
            return true;
        }

        /// <summary>
        /// Closes the door. Does not check if this is possible.
        /// </summary>
        public void Close()
        {
            State = DoorState.Closing;
            _openTimeCounter = 0;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();
            Owner.SpawnTimer(CloseTimeOne, async () =>
            {
                // if somebody walked into the door as it was closing, and we don't crush things
                if (!SafetyCheck())
                {
                    Open();
                    return;
                }

                OnPartialClose();
                await Timer.Delay(CloseTimeTwo, _cancellationTokenSource.Token);
                if (Occludes && Owner.TryGetComponent(out OccluderComponent? occluder))
                {
                    occluder.Enabled = true;
                }

                State = DoorState.Closed;
            }, _cancellationTokenSource.Token);
        }

        protected override void OnPartialClose()
        {
            base.OnPartialClose();

            var becomeAirtight = true;
            if (!Safety)
            {
                // crushes people inside of the door, temporarily turning off collisions with them while doing so, to avoid jank
                becomeAirtight = !TryCrush();
            }
            if (becomeAirtight && Owner.TryGetComponent(out AirtightComponent? airtight))
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
            if (!Owner.TryGetComponent(out IPhysicsComponent? physics))
                return false;

            // Crush
            foreach (var e in physics.GetCollidingEntities(Vector2.Zero, false))
            {
                if (!e.TryGetComponent(out StunnableComponent? stun)
                    || !e.TryGetComponent(out IDamageableComponent? damage)
                    || !e.TryGetComponent(out IPhysicsComponent? otherBody))
                    continue;

                var percentage = otherBody.WorldAABB.IntersectPercentage(physics.WorldAABB);

                if (percentage < 0.1f)
                    continue;

                CurrentlyCrushing = e.Uid;

                damage.ChangeDamage(DamageType.Blunt, DoorCrushDamage, false, Owner);
                stun.Paralyze(DoorStunTime);

                // If we hit someone, open up after stun (opens right when stun ends)
                Owner.SpawnTimer(TimeSpan.FromSeconds(DoorStunTime) - OpenTimeOne - OpenTimeTwo, Open);
                return true;
            }
            return false;
        }

        #endregion

        public void Deny()
        {
            if (Owner.TryGetComponent(out IDoorCheck? doorCheck) && !doorCheck.DenyCheck())
            {
                return;
            }

            if (State == DoorState.Open || IsWeldedShut)
                return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();
            SetAppearance(DoorVisualState.Deny);
            Owner.SpawnTimer(DenyTime, () =>
            {
                SetAppearance(DoorVisualState.Closed);
            }, _cancellationTokenSource.Token);
        }

        public override void OnUpdate(float frameTime)
        {
            if (State != DoorState.Open)
            {
                return;
            }

            if (AutoClose)
            {
                _openTimeCounter += frameTime;
            }

            var realCloseTime = AutoCloseDelay;

            if(Owner.TryGetComponent(out IDoorCheck? doorCheck))
            {
                realCloseTime = doorCheck.GetCloseSpeed() ?? realCloseTime;
            }

            if (_openTimeCounter > realCloseTime)
            {
                if (CanCloseGeneric())
                {
                    // Close() resets OpenTimeCounter to 0, so it's fine.
                    Close();
                }
                else
                {
                    // Try again in 2 seconds if it's jammed or something.
                    _openTimeCounter -= 2;
                }
            }
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if(!eventArgs.Using.TryGetComponent(out ToolComponent? tool))
            {
                return false;
            }

            // for prying doors
            if (tool.HasQuality(ToolQuality.Prying) && !IsWeldedShut)
            {
                var successfulPry = false;

                if (Owner.TryGetComponent(out IDoorCheck? doorCheck))
                {
                    doorCheck.OnStartPry(eventArgs);
                    successfulPry = await tool.UseTool(eventArgs.User, Owner,
                        doorCheck.GetPryTime() ?? 0.2f, ToolQuality.Prying, () => doorCheck.CanPryCheck(eventArgs));
                }
                else
                {
                    successfulPry = await tool.UseTool(eventArgs.User, Owner, 0.2f, ToolQuality.Prying);
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
            return new DoorComponentState(State, StateChangeStartTime, GameTiming.CurTime, CurrentlyCrushing);
        }
    }
}
