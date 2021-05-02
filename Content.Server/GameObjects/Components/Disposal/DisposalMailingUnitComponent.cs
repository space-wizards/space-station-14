#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems.DeviceNetwork;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Disposal;
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
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalMailingUnitComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class DisposalMailingUnitComponent : SharedDisposalMailingUnitComponent, IInteractHand, IActivate, IInteractUsing, IDragDropOn
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const string HolderPrototypeId = "DisposalHolder";

        /// <summary>
        ///     The delay for an entity trying to move out of this unit.
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit.
        /// </summary>
        [ViewVariables]
        private TimeSpan _lastExitAttempt;

        public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

        /// <summary>
        ///     The current pressure of this disposal unit.
        ///     Prevents it from flushing if it is not equal to or bigger than 1.
        /// </summary>
        [ViewVariables]
        [DataField("pressure")]
        private float _pressure = 1f;

        private bool _engaged;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoEngageTime")]
        private readonly TimeSpan _automaticEngageTime = TimeSpan.FromSeconds(30);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flushDelay")]
        private readonly TimeSpan _flushDelay = TimeSpan.FromSeconds(3);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("entryDelay")]
        private float _entryDelay = 0.5f;

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

        [ViewVariables]
        private WiredNetworkConnection? _connection;

        [ViewVariables] public IReadOnlyList<IEntity> ContainedEntities => _container.ContainedEntities;

        [ViewVariables]
        private readonly List<string> _targetList = new();

        [ViewVariables]
        private string? _target;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("Tag")]
        private string _tag = string.Empty;

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

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalMailingUnitUiKey.Key);

        /// <summary>
        ///     Store the translated state.
        /// </summary>
        private (PressureState State, string Localized) _locState;

        public override bool CanInsert(IEntity entity)
        {
            if (!Anchored)
            {
                return false;
            }

            if (!entity.TryGetComponent(out IPhysBody? physics) ||
                !physics.CanCollide)
            {
                return false;
            }

            if (!entity.HasComponent<ItemComponent>() &&
                !entity.HasComponent<IBody>())
            {
                return false;
            }
            return _container.CanInsert(entity);
        }

        private void TryQueueEngage()
        {
            if (!Powered && ContainedEntities.Count == 0)
            {
                return;
            }

            _automaticEngageToken = new CancellationTokenSource();

            Timer.Spawn(_automaticEngageTime, () =>
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

            if (user != null && _entryDelay > 0f)
            {
                var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                var doAfterArgs = new DoAfterEventArgs(user, _entryDelay, default, Owner)
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
                Timer.Spawn(_flushDelay, () => TryFlush());
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
            var entities = _container.ContainedEntities.ToList();
            foreach (var entity in _container.ContainedEntities.ToList())
            {
                _container.Remove(entity);
            }

            if (_target == null)
            {
                return false;
            }

            var holder = CreateTaggedHolder(entities, _target);

            entryComponent.TryInsert(holder);

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            _pressure = 0;

            Engaged = false;

            UpdateVisualState(true);
            UpdateInterface();

            if (_connection != null)
            {
                var data = new Dictionary<string, string>
                {
                    { NetworkUtils.COMMAND, NET_CMD_SENT },
                    { NET_SRC, _tag },
                    { NET_TARGET, _target }
                };

                _connection.Broadcast(_connection.Frequency, data);
            }

            return true;
        }

        private DisposalHolderComponent CreateTaggedHolder(IReadOnlyCollection<IEntity> entities, string tag)
        {
            var holder = Owner.EntityManager.SpawnEntity(HolderPrototypeId, Owner.Transform.MapPosition);
            var holderComponent = holder.GetComponent<DisposalHolderComponent>();

            holderComponent.Tags.Add(tag);
            holderComponent.Tags.Add(TAGS_MAIL);

            foreach (var entity in entities)
            {
                holderComponent.TryInsert(entity);
            }

            return holderComponent;
        }

        private void UpdateTargetList()
        {
            _targetList.Clear();
            var payload = new Dictionary<string, string>
            {
                { NetworkUtils.COMMAND, NET_CMD_REQUEST }
            };

            _connection?.Broadcast(_connection.Frequency, payload);
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

            if (obj.Message is UiButtonPressedMessage buttonMessage)
            {
                switch (buttonMessage.Button)
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

            if (obj.Message is UiTargetUpdateMessage tagMessage && TagRegex.IsMatch(tagMessage.Target ?? string.Empty))
            {
                _target = tagMessage.Target;
            }
        }

        private void OnConfigUpdate(Dictionary<string, string> config)
        {
            if (config.TryGetValue("Tag", out var tag))
                _tag = tag;
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

            _connection = new WiredNetworkConnection(OnReceiveNetMessage, false, Owner);
            UpdateInterface();
        }

        protected override void Startup()
        {
            base.Startup();

            if(!Owner.HasComponent<AnchorableComponent>())
            {
                Logger.WarningS("VitalComponentMissing", $"Disposal unit {Owner.Uid} is missing an anchorable component");
            }

            UpdateTargetList();
            UpdateVisualState();
            UpdateInterface();
        }

        public override void OnRemove()
        {
            if (_container != null)
            {
                foreach (var entity in _container.ContainedEntities.ToArray())
                {
                    _container.ForceRemove(entity);
                }
            }

            UserInterface?.CloseAll();

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            _container = null!;

            _connection!.Close();

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case SharedConfigurationComponent.ConfigUpdatedComponentMessage msg:
                    OnConfigUpdate(msg.Config);
                    break;
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

        private void OnReceiveNetMessage(int frequency, string sender, IReadOnlyDictionary<string, string> payload, object _, bool broadcast)
        {
            if (payload.TryGetValue(NetworkUtils.COMMAND, out var command) && Powered)
            {
                if (command == NET_CMD_RESPONSE && payload.TryGetValue(NET_TAG, out var tag))
                {
                    _targetList.Add(tag);
                    UpdateInterface();
                }

                if (command == NET_CMD_REQUEST)
                {
                    if (_tag == "" || !Powered)
                        return;

                    var data = new Dictionary<string, string>
                    {
                        {NetworkUtils.COMMAND, NET_CMD_RESPONSE},
                        {NET_TAG, _tag}
                    };

                    _connection?.Send(frequency, sender, data);
                }
            }
        }

        private bool IsValidInteraction(ITargetedInteractEventArgs eventArgs)
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
            if (eventArgs.User == null)
            {
                return false;
            }

            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }

            // Duplicated code here, not sure how else to get actor inside to make UserInterface happy.

            if (IsValidInteraction(eventArgs))
            {
                UpdateTargetList();
                UpdateInterface();
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
            return CanInsert(eventArgs.Dragged);
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            _ = TryInsert(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<DisposalMailingUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalMailingUnitComponent component, VerbData data)
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

            protected override void Activate(IEntity user, DisposalMailingUnitComponent component)
            {
                _ = component.TryInsert(user, user);
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalMailingUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalMailingUnitComponent component, VerbData data)
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

            protected override void Activate(IEntity user, DisposalMailingUnitComponent component)
            {
                component.Engaged = true;
                component.TryFlush();
            }
        }
    }
}
