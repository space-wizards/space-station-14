#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent, IInteractHand, IActivate, IInteractUsing, IThrowCollide, IGasMixtureHolder
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "DisposalUnit";

        /// <summary>
        ///     The delay for an entity trying to move out of this unit.
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit.
        /// </summary>
        [ViewVariables]
        private TimeSpan _lastExitAttempt;

        /// <summary>
        ///     The current pressure of this disposal unit.
        ///     Prevents it from flushing if it is not equal to or bigger than 1.
        /// </summary>
        [ViewVariables]
        [DataField("pressure")]
        private float _pressure;

        private bool _engaged;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoEngageTime")]
        private readonly TimeSpan _automaticEngageTime = TimeSpan.FromSeconds(30);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flushDelay")]
        private readonly TimeSpan _flushDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        ///     Delay from trying to enter disposals ourselves.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("entryDelay")]
        private float _entryDelay = 0.5f;

        /// <summary>
        ///     Delay from trying to shove someone else into disposals.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _draggedEntryDelay = 0.5f;

        /// <summary>
        ///     Token used to cancel the automatic engage of a disposal unit
        ///     after an entity enters it.
        /// </summary>
        private CancellationTokenSource? _automaticEngageToken;

        /// <summary>
        ///     Container of entities inside this disposal unit.
        /// </summary>
        [ViewVariables]
        private Container _container = default!;

        [ViewVariables] public IReadOnlyList<IEntity> ContainedEntities => _container.ContainedEntities;

        [ViewVariables]
        public bool Powered =>
            !Owner.TryGetComponent(out PowerReceiverComponent? receiver) ||
            receiver.Powered;

        [ViewVariables]
        private PressureState State => _pressure >= 1 ? PressureState.Ready : PressureState.Pressurizing;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool Engaged
        {
            get => _engaged;
            set
            {
                var oldEngaged = _engaged;
                _engaged = value;

                if (oldEngaged == value)
                {
                    return;
                }

                UpdateVisualState();
            }
        }

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalUnitUiKey.Key);

        [DataField("air")]
        public GasMixture Air { get; set; } = new GasMixture(Atmospherics.CellVolume);

        public override bool CanInsert(IEntity entity)
        {
            if (!base.CanInsert(entity))
                return false;

            return _container.CanInsert(entity);
        }

        private void TryQueueEngage()
        {
            if (!Powered && ContainedEntities.Count == 0)
            {
                return;
            }

            _automaticEngageToken = new CancellationTokenSource();

            Owner.SpawnTimer(_automaticEngageTime, () =>
            {
                if (!TryFlush())
                {
                    TryQueueEngage();
                }
            }, _automaticEngageToken.Token);
        }

        private void AfterInsert(IEntity entity)
        {
            TryQueueEngage();

            if (entity.TryGetComponent(out IActorComponent? actor))
            {
                UserInterface?.Close(actor.playerSession);
            }

            UpdateVisualState();
        }

        public async Task<bool> TryInsert(IEntity entity, IEntity? user = default)
        {
            if (!CanInsert(entity))
                return false;

            var delay = user == entity ? _entryDelay : _draggedEntryDelay;

            if (user != null && delay > 0.0f)
            {
                var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                // Can't check if our target AND disposals moves currently so we'll just check target.
                // if you really want to check if disposals moves then add a predicate.
                var doAfterArgs = new DoAfterEventArgs(user, delay, default, entity)
                {
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = false,
                };

                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;
            }

            if (!_container.Insert(entity))
                return false;

            AfterInsert(entity);

            return true;
        }

        private bool TryDrop(IEntity user, IEntity entity)
        {
            if (!user.TryGetComponent(out HandsComponent? hands))
            {
                return false;
            }

            if (!CanInsert(entity) || !hands.Drop(entity, _container))
            {
                return false;
            }

            AfterInsert(entity);

            return true;
        }

        private void Remove(IEntity entity)
        {
            _container.Remove(entity);

            if (ContainedEntities.Count == 0)
            {
                _automaticEngageToken?.Cancel();
                _automaticEngageToken = null;
            }

            UpdateVisualState();
        }

        private bool CanFlush()
        {
            return _pressure >= 1 && Powered && Anchored;
        }

        private void ToggleEngage()
        {
            Engaged ^= true;

            if (Engaged && CanFlush())
            {
                Owner.SpawnTimer(_flushDelay, () => TryFlush());
            }
        }

        public bool TryFlush()
        {
            if (!CanFlush())
            {
                return false;
            }

            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var coords = Owner.Transform.Coordinates;
            var entry = grid.GetLocal(coords)
                .FirstOrDefault(entity => Owner.EntityManager.ComponentManager.HasComponent<DisposalEntryComponent>(entity));

            if (entry == default)
            {
                return false;
            }

            var entryComponent = Owner.EntityManager.ComponentManager.GetComponent<DisposalEntryComponent>(entry);

            if (Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos) &&
                tileAtmos.Air != null &&
                tileAtmos.Air.Temperature > 0)
            {
                var tileAir = tileAtmos.Air;
                var transferMoles = 0.1f * (0.05f * Atmospherics.OneAtmosphere * 1.01f - Air.Pressure) * Air.Volume / (tileAir.Temperature * Atmospherics.R);

                Air = tileAir.Remove(transferMoles);

                var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
                atmosSystem
                    .GetGridAtmosphere(Owner.Transform.Coordinates)?
                    .Invalidate(tileAtmos.GridIndices);
            }

            entryComponent.TryInsert(this);

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            _pressure = 0;

            Engaged = false;

            UpdateVisualState(true);
            UpdateInterface();

            return true;
        }

        private void TryEjectContents()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                Remove(entity);
            }
        }

        private void TogglePower()
        {
            if (!Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                return;
            }

            receiver.PowerDisabled = !receiver.PowerDisabled;
            UpdateInterface();
        }

        private void UpdateInterface()
        {
            string stateString;

            stateString = Loc.GetString($"{State}");
            var state = new DisposalUnitBoundUserInterfaceState(Owner.Name, stateString, _pressure, Powered, Engaged);
            UserInterface?.SetState(state);
        }

        private bool PlayerCanUse(IEntity? player)
        {
            if (player == null)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(player) ||
                !ActionBlockerSystem.CanUse(player))
            {
                return false;
            }

            return true;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            if (!PlayerCanUse(obj.Session.AttachedEntity))
            {
                return;
            }

            if (obj.Message is not UiButtonPressedMessage message)
            {
                return;
            }

            switch (message.Button)
            {
                case UiButton.Eject:
                    TryEjectContents();
                    break;
                case UiButton.Engage:
                    ToggleEngage();
                    break;
                case UiButton.Power:
                    TogglePower();
                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateVisualState()
        {
            UpdateVisualState(false);
        }

        private void UpdateVisualState(bool flush)
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            if (!Anchored)
            {
                appearance.SetData(Visuals.VisualState, VisualState.UnAnchored);
                appearance.SetData(Visuals.Handle, HandleState.Normal);
                appearance.SetData(Visuals.Light, LightState.Off);
                return;
            }
            else if (_pressure < 1)
            {
                appearance.SetData(Visuals.VisualState, VisualState.Charging);
            }
            else
            {
                appearance.SetData(Visuals.VisualState, VisualState.Anchored);
            }

            appearance.SetData(Visuals.Handle, Engaged
                ? HandleState.Engaged
                : HandleState.Normal);

            if (!Powered)
            {
                appearance.SetData(Visuals.Light, LightState.Off);
                return;
            }

            if (flush)
            {
                appearance.SetData(Visuals.VisualState, VisualState.Flushing);
                appearance.SetData(Visuals.Light, LightState.Off);
                return;
            }

            if (ContainedEntities.Count > 0)
            {
                appearance.SetData(Visuals.Light, LightState.Full);
                return;
            }

            appearance.SetData(Visuals.Light, _pressure < 1
                ? LightState.Charging
                : LightState.Ready);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (!Powered)
            {
                return;
            }

            var oldPressure = _pressure;

            _pressure = _pressure + frameTime > 1
                ? 1
                : _pressure + 0.05f * frameTime;

            if (oldPressure < 1 && _pressure >= 1)
            {
                UpdateVisualState();

                if (Engaged)
                {
                    TryFlush();
                }
            }

            // TODO: Ideally we'd just send the start and end and client could lerp as the bandwidth would be way lower
            if (_pressure < 1.0f || oldPressure < 1.0f && _pressure >= 1.0f)
            {
                UpdateInterface();
            }
        }

        private void PowerStateChanged(PowerChangedMessage args)
        {
            if (!args.Powered)
            {
                _automaticEngageToken?.Cancel();
                _automaticEngageToken = null;
            }

            UpdateVisualState();

            if (Engaged && !TryFlush())
            {
                TryQueueEngage();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _container = ContainerHelpers.EnsureContainer<Container>(Owner, Name);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateInterface();
        }

        protected override void Startup()
        {
            base.Startup();

            if(!Owner.HasComponent<AnchorableComponent>())
            {
                Logger.WarningS("VitalComponentMissing", $"Disposal unit {Owner.Uid} is missing an anchorable component");
            }

            UpdateVisualState();
            UpdateInterface();
        }

        public override void OnRemove()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                _container.ForceRemove(entity);
            }

            UserInterface?.CloseAll();

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            _container = null!;

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (!msg.Entity.TryGetComponent(out HandsComponent? hands) ||
                        hands.Count == 0 ||
                        _gameTiming.CurTime < _lastExitAttempt + ExitAttemptDelay)
                    {
                        break;
                    }

                    _lastExitAttempt = _gameTiming.CurTime;
                    Remove(msg.Entity);
                    break;

                case AnchoredChangedMessage:
                    UpdateVisualState();
                    break;

                case PowerChangedMessage powerChanged:
                    PowerStateChanged(powerChanged);
                    break;
            }
        }

        bool IsValidInteraction(ITargetedInteractEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't do that!"));
                return false;
            }

            if (eventArgs.User.IsInContainer())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't reach there!"));
                return false;
            }
            // This popup message doesn't appear on clicks, even when code was seperate. Unsure why.

            if (!eventArgs.User.HasComponent<IHandsComponent>())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands!"));
                return false;
            }

            return true;
        }


        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }
            // Duplicated code here, not sure how else to get actor inside to make UserInterface happy.

            if (IsValidInteraction(eventArgs))
            {
                UserInterface?.Open(actor.playerSession);
                return true;
            }

            return false;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (IsValidInteraction(eventArgs))
            {
                UserInterface?.Open(actor.playerSession);
            }

            return;
        }


        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryDrop(eventArgs.User, eventArgs.Using);
        }

        public override bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            // Base is redundant given this already calls the base CanInsert
            // If that changes then update this
            return CanInsert(eventArgs.Dragged);
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            _ = TryInsert(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        void IThrowCollide.HitBy(ThrowCollideEventArgs eventArgs)
        {
            if (!CanInsert(eventArgs.Thrown) ||
                IoCManager.Resolve<IRobustRandom>().NextDouble() > 0.75 ||
                !_container.Insert(eventArgs.Thrown))
            {
                return;
            }

            AfterInsert(eventArgs.Thrown);
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.ContainedEntities.Contains(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Jump inside");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                _ = component.TryInsert(user, user);
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.ContainedEntities.Contains(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Flush");
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.Engaged = true;
                component.TryFlush();
            }
        }
    }
}
