using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Access;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Hands.Components;
using Content.Server.Stunnable;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Doors.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedDoorComponent))]
    public class ServerDoorComponent : SharedDoorComponent, IActivate, IInteractUsing, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        [DataField("board", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _boardPrototype;

        [DataField("weldingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _weldingQuality = "Welding";

        [DataField("pryingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _pryingQuality = "Prying";

        [DataField("tryOpenDoorSound")]
        private SoundSpecifier _tryOpenDoorSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

        [DataField("crushDamage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier CrushDamage = default!;

        [DataField("changeAirtight")]
        public bool ChangeAirtight = true;

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

                _entMan.EventBus.RaiseLocalEvent(Owner, new DoorStateChangedEvent(State), false);
                _autoCloseCancelTokenSource?.Cancel();

                Dirty();
            }
        }

        private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5);

        private CancellationTokenSource? _stateChangeCancelTokenSource;
        private CancellationTokenSource? _autoCloseCancelTokenSource;

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
        public bool BumpOpen = true;

        /// <summary>
        /// Whether the door will open when it is activated or clicked.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("clickOpen")]
        public bool ClickOpen = true;

        /// <summary>
        /// Whether the door starts open when it's first loaded from prototype. A door won't start open if its prototype is also welded shut.
        /// Handled in Startup().
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("startOpen")]
        private bool _startOpen = false;

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
        /// Sound to play when the door opens.
        /// </summary>
        [DataField("openSound")]
        public SoundSpecifier? OpenSound;

        /// <summary>
        /// Sound to play when the door closes.
        /// </summary>
        [DataField("closeSound")]
        public SoundSpecifier? CloseSound;

        /// <summary>
        /// Sound to play if the door is denied.
        /// </summary>
        [DataField("denySound")]
        public SoundSpecifier? DenySound;

        /// <summary>
        ///     Should this door automatically close if its been open for too long?
        /// </summary>
        [DataField("autoClose")]
        public bool AutoClose = true;

        /// <summary>
        /// Default time that the door should take to pry open.
        /// </summary>
        [DataField("pryTime")]
        public float PryTime = 1.5f;

        /// <summary>
        ///     Minimum interval allowed between deny sounds in milliseconds.
        /// </summary>
        [DataField("denySoundMinimumInterval")]
        public float DenySoundMinimumInterval = 450.0f;

        /// <summary>
        ///     Used to stop people from spamming the deny sound.
        /// </summary>
        private TimeSpan LastDenySoundTime = TimeSpan.Zero;

        /// <summary>
        /// Whether the door can currently be welded.
        /// </summary>
        private bool CanWeldShut => _weldable && State == DoorState.Closed;

        /// <summary>
        ///     Whether something is currently using a welder on this so DoAfter isn't spammed.
        /// </summary>
        private bool _beingWelded;


        //[ViewVariables(VVAccess.ReadWrite)]
        //[DataField("canCrush")]
        //private bool _canCrush = true; // TODO implement door crushing

        protected override void Startup()
        {
            base.Startup();

            if (IsWeldedShut)
            {
                if (!CanWeldShut)
                {
                    Logger.Warning("{0} prototype loaded with incompatible flags: 'welded' is true, but door cannot be welded.", _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
                    return;
                }
                SetAppearance(DoorVisualState.Welded);
            }

            CreateDoorElectronicsBoard();
        }

        protected override void OnRemove()
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
                    Logger.Warning("{0} prototype loaded with incompatible flags: 'welded' and 'startOpen' are both true.", _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
                    return;
                }
                QuickOpen(false);
            }

            CreateDoorElectronicsBoard();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!ClickOpen)
                return;

            var ev = new DoorClickShouldActivateEvent(eventArgs);
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            if (ev.Handled)
                return;

            if (State == DoorState.Open)
            {
                TryClose(eventArgs.User);
            }
            else if (State == DoorState.Closed)
            {
                TryOpen(eventArgs.User);
            }
        }

        #region Opening

        public void TryOpen(EntityUid? user = null)
        {
            var msg = new DoorOpenAttemptEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, msg);

            if (msg.Cancelled) return;

            if (user == null)
            {
                // a machine opened it or something, idk
                Open();
            }
            else if (CanOpenByEntity(user.Value))
            {
                Open();

                if (_entMan.TryGetComponent(user, out HandsComponent? hands) && hands.Count == 0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _tryOpenDoorSound.GetSound(), Owner,
                        AudioParams.Default.WithVolume(-2));
                }
            }
            else
            {
                Deny();
            }
        }

        public bool CanOpenByEntity(EntityUid user)
        {
            if (!CanOpenGeneric())
            {
                return false;
            }

            if (!_entMan.TryGetComponent(Owner, out AccessReaderComponent? access))
            {
                return true;
            }

            var doorSystem = EntitySystem.Get<DoorSystem>();
            var isAirlockExternal = HasAccessType("External");

            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            return doorSystem.AccessType switch
            {
                DoorSystem.AccessTypes.AllowAll => true,
                DoorSystem.AccessTypes.AllowAllIdExternal => isAirlockExternal || accessSystem.IsAllowed(access, user),
                DoorSystem.AccessTypes.AllowAllNoExternal => !isAirlockExternal,
                _ => accessSystem.IsAllowed(access, user)
            };
        }

        /// <summary>
        /// Returns whether a door has a certain access type. For example, maintenance doors will have access type
        /// "Maintenance" in their AccessReader.
        /// </summary>
        private bool HasAccessType(string accessType)
        {
            if (_entMan.TryGetComponent(Owner, out AccessReaderComponent? access))
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

            var ev = new BeforeDoorOpenedEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            return !ev.Cancelled;
        }

        /// <summary>
        /// Opens the door. Does not check if this is possible.
        /// </summary>
        public void Open()
        {
            State = DoorState.Opening;
            if (Occludes && _entMan.TryGetComponent(Owner, out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }

            if (ChangeAirtight && _entMan.TryGetComponent(Owner, out AirtightComponent? airtight))
            {
                EntitySystem.Get<AirtightSystem>().SetAirblocked(airtight, false);
            }

            _stateChangeCancelTokenSource?.Cancel();
            _stateChangeCancelTokenSource = new();

            if (OpenSound != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), OpenSound.GetSound(), Owner,
                    AudioParams.Default.WithVolume(-5));
            }

            Owner.SpawnTimer(OpenTimeOne, async () =>
            {
                OnPartialOpen();
                await Timer.Delay(OpenTimeTwo, _stateChangeCancelTokenSource.Token);

                State = DoorState.Open;
                RefreshAutoClose();
            }, _stateChangeCancelTokenSource.Token);
        }

        protected override void OnPartialOpen()
        {
            base.OnPartialOpen();

            if (ChangeAirtight && _entMan.TryGetComponent(Owner, out AirtightComponent? airtight))
            {
                EntitySystem.Get<AirtightSystem>().SetAirblocked(airtight, false);
            }

            _entMan.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, false));
        }

        private void QuickOpen(bool refresh)
        {
            if (Occludes && _entMan.TryGetComponent(Owner, out OccluderComponent? occluder))
            {
                occluder.Enabled = false;
            }
            OnPartialOpen();
            State = DoorState.Open;
            if(refresh)
                RefreshAutoClose();
        }

        #endregion

        #region Closing

        public void TryClose(EntityUid? user = null)
        {
            var msg = new DoorCloseAttemptEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, msg);

            if (msg.Cancelled) return;

            if (user != null && !CanCloseByEntity(user.Value))
            {
                Deny();
                return;
            }

            Close();
        }

        public bool CanCloseByEntity(EntityUid user)
        {
            if (!CanCloseGeneric())
            {
                return false;
            }

            if (!_entMan.TryGetComponent(Owner, out AccessReaderComponent? access))
            {
                return true;
            }

            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            return accessSystem.IsAllowed(access, user);
        }

        /// <summary>
        /// Checks if we can close at all, for anyone or anything. Will return false if inhibited by an IDoorCheck component or if we are colliding with somebody while our Safety is on.
        /// </summary>
        /// <returns>Boolean describing whether this door can close.</returns>
        public bool CanCloseGeneric()
        {
            var ev = new BeforeDoorClosedEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            if (ev.Cancelled)
                return false;

            return !IsSafetyColliding();
        }

        private bool SafetyCheck()
        {
            var ev = new DoorSafetyEnabledEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            return ev.Safety || _inhibitCrush;
        }

        /// <summary>
        /// Checks if we care about safety, and if so, if something is colliding with it; ignores the CanCollide of the door's PhysicsComponent.
        /// </summary>
        /// <returns>True if something is colliding with us and we shouldn't crush things, false otherwise.</returns>
        private bool IsSafetyColliding()
        {
            var safety = SafetyCheck();

            if (safety && _entMan.TryGetComponent(Owner, out PhysicsComponent? physicsComponent))
            {
                var broadPhaseSystem = EntitySystem.Get<SharedPhysicsSystem>();

                // Use this version so we can ignore the CanCollide being false
                foreach(var _ in broadPhaseSystem.GetCollidingEntities(physicsComponent, -0.015f))
                {
                    return true;
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

            if (CloseSound != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), CloseSound.GetSound(), Owner,
                    AudioParams.Default.WithVolume(-5));
            }

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

                if (Occludes && _entMan.TryGetComponent(Owner, out OccluderComponent? occluder))
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
            var becomeairtight = ChangeAirtight && (SafetyCheck() || !TryCrush());

            if (becomeairtight && _entMan.TryGetComponent(Owner, out AirtightComponent? airtight))
            {
                EntitySystem.Get<AirtightSystem>().SetAirblocked(airtight, true);
            }

            _entMan.EventBus.RaiseEvent(EventSource.Local, new AccessReaderChangeMessage(Owner, true));
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
                var percentage = e.GetWorldAABB().IntersectPercentage(doorAABB);

                if (percentage < 0.1f)
                    continue;

                hitsomebody = true;
                CurrentlyCrushing.Add(e.Owner);

                if (_entMan.HasComponent<DamageableComponent>(e.Owner))
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(e.Owner, CrushDamage);

                EntitySystem.Get<StunSystem>().TryParalyze(e.Owner, TimeSpan.FromSeconds(DoorStunTime), true);
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
            var ev = new BeforeDoorDeniedEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            if (ev.Cancelled)
                return;

            if (State == DoorState.Open || IsWeldedShut)
                return;

            if (DenySound != null)
            {
                if (LastDenySoundTime == TimeSpan.Zero)
                {
                    LastDenySoundTime = _gameTiming.CurTime;
                }
                else
                {
                    var difference = _gameTiming.CurTime - LastDenySoundTime;
                    if (difference < TimeSpan.FromMilliseconds(DenySoundMinimumInterval))
                        return;
                }

                LastDenySoundTime = _gameTiming.CurTime;
                SoundSystem.Play(Filter.Pvs(Owner), DenySound.GetSound(), Owner,
                    AudioParams.Default.WithVolume(-3));
            }

            _stateChangeCancelTokenSource?.Cancel();
            _stateChangeCancelTokenSource = new();
            SetAppearance(DoorVisualState.Deny);
            Owner.SpawnTimer(DenyTime, () =>
            {
                SetAppearance(DoorVisualState.Closed);
            }, _stateChangeCancelTokenSource.Token);
        }

        /// <summary>
        /// Starts a new auto close timer if this is appropriate
        /// (i.e. event raised is not cancelled).
        /// </summary>
        public void RefreshAutoClose()
        {
            if (State != DoorState.Open)
                return;

            if (!AutoClose)
                return;

            var autoev = new BeforeDoorAutoCloseEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, autoev, false);
            if (autoev.Cancelled)
                return;

            _autoCloseCancelTokenSource = new();

            var ev = new DoorGetCloseTimeModifierEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);
            var realCloseTime = AutoCloseDelay * ev.CloseTimeModifier;

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
            if(!_entMan.TryGetComponent(eventArgs.Using, out ToolComponent? tool))
            {
                return false;
            }

            var toolSystem = EntitySystem.Get<ToolSystem>();

            // for prying doors
            if (tool.Qualities.Contains(_pryingQuality) && !IsWeldedShut)
            {
                var ev = new DoorGetPryTimeModifierEvent();
                _entMan.EventBus.RaiseLocalEvent(Owner, ev, false);

                var canEv = new BeforeDoorPryEvent(eventArgs);
                _entMan.EventBus.RaiseLocalEvent(Owner, canEv, false);

                if (canEv.Cancelled) return false;

                var successfulPry = await toolSystem.UseTool(eventArgs.Using, eventArgs.User, Owner,
                        0f, ev.PryTimeModifier * PryTime, _pryingQuality);

                if (successfulPry && !IsWeldedShut)
                {
                    _entMan.EventBus.RaiseLocalEvent(Owner, new OnDoorPryEvent(eventArgs), false);
                    if (State == DoorState.Closed)
                    {
                        Open();
                    }
                    else if (State == DoorState.Open)
                    {
                        Close();
                    }
                }

                // regardless of whether this action actually succeeded, it generated a do-after bar. So prevent other
                // interactions from happening afterwards by returning true.
                return true;
            }

            // for welding doors
            if (_beingWelded || !CanWeldShut || !_entMan.TryGetComponent(tool.Owner, out WelderComponent? welder) || !welder.Lit)
            {
                // no interaction occurred
                return false;
            }
            
            _beingWelded = true;

            // perform a do-after delay
            var result = await toolSystem.UseTool(eventArgs.Using, eventArgs.User, Owner, 3f, 3f, _weldingQuality, () => CanWeldShut);

            // if successful, toggle the weld-status (while also ensuring that it can still be welded shut after the delay)
            if (result)
                IsWeldedShut = CanWeldShut && !IsWeldedShut;

            _beingWelded = false;
            return true;
        }

        /// <summary>
        ///     Creates the corresponding door electronics board on the door.
        ///     This exists so when you deconstruct doors that were serialized with the map,
        ///     you can retrieve the door electronics board.
        /// </summary>
        private void CreateDoorElectronicsBoard()
        {
            // Ensure that the construction component is aware of the board container.
            if (_entMan.TryGetComponent(Owner, out ConstructionComponent? construction))
                EntitySystem.Get<ConstructionSystem>().AddContainer(Owner, "board", construction);

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

        public override ComponentState GetComponentState()
        {
            return new DoorComponentState(State, StateChangeStartTime, CurrentlyCrushing, GameTiming.CurTime);
        }
    }
}
