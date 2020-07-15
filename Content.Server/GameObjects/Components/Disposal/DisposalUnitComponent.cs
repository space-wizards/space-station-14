using System;
using System.Linq;
using System.Threading;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IInteractUsing))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent, IInteractHand, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "DisposalUnit";

        /// <summary>
        ///     The delay for an entity trying to move out of this unit
        /// </summary>
        private static readonly TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit
        /// </summary>
        private TimeSpan _lastExitAttempt;

        /// <summary>
        ///     The time that it takes this disposal unit to flush its contents
        /// </summary>
        [ViewVariables]
        private TimeSpan _flushTime;

        /// <summary>
        ///     Token used to cancel delayed appearance changes
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        ///     Container of entities inside this disposal unit
        /// </summary>
        [ViewVariables]
        private Container _container;

        [ViewVariables]
        private BoundUserInterface _userInterface;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent receiver) || !receiver.PowerDisabled;

        private bool CanInsert(IEntity entity)
        {
            return Powered &&
                   entity.HasComponent<DisposableComponent>() &&
                   _container.CanInsert(entity);
        }

        public bool TryInsert(IEntity entity)
        {
            return CanInsert(entity) && _container.Insert(entity);
        }

        private void Remove(IEntity entity)
        {
            _container.Remove(entity);
        }

        private bool CanFlush()
        {
            return (!Owner.TryGetComponent(out PowerReceiverComponent receiver) ||
                    receiver.Powered) &&
                   (!Owner.TryGetComponent(out PhysicsComponent physics) ||
                    physics.Anchored);
        }

        private void TryFlush()
        {
            if (!CanFlush())
            {
                return;
            }

            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var entry = snapGrid
                .GetLocal()
                .FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entry == null)
            {
                return;
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();
            foreach (var entity in _container.ContainedEntities.ToList())
            {
                _container.Remove(entity);
                entryComponent.TryInsert(entity);
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(Visuals.VisualState, VisualState.Flushing);
                Timer.Spawn(_flushTime, UpdateVisualState, _cancellationTokenSource.Token);
            }

            UpdateInterface();
        }

        private void TryEject(IEntity entity)
        {
            _container.Remove(entity);
        }

        private void TryEjectContents()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                TryEject(entity);
            }
        }

        private void TogglePower()
        {
            if (!Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                return;
            }

            receiver.PowerDisabled = !receiver.PowerDisabled;
            UpdateInterface();
        }

        private DisposalUnitBoundUserInterfaceState GetInterfaceState()
        {
            return new DisposalUnitBoundUserInterfaceState(Owner.Name, 1, Powered);
        }

        private void UpdateInterface()
        {
            var state = GetInterfaceState();
            _userInterface.SetState(state);
        }

        private bool PlayerCanUse(IEntity player)
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
            if (!PlayerCanUse(obj.Session.AttachedEntity))
            {
                return;
            }

            if (!(obj.Message is UiButtonPressedMessage message))
            {
                return;
            }

            switch (message.Button)
            {
                case UiButton.Eject:
                    TryEjectContents();
                    break;
                case UiButton.Engage:
                    TryFlush();
                    break;
                case UiButton.Power:
                    TogglePower();

                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateVisualState()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                return;
            }

            if (!Owner.TryGetComponent(out PhysicsComponent physics) ||
                physics.Anchored)
            {
                appearance.SetData(Visuals.VisualState, VisualState.Anchored);
                return;
            }

            appearance.SetData(Visuals.VisualState, VisualState.UnAnchored);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var flushSeconds = 2;
            serializer.DataField(ref flushSeconds, "flushTime", 2);

            _flushTime = TimeSpan.FromSeconds(flushSeconds);
        }

        public override void Initialize()
        {
            base.Initialize();

            _cancellationTokenSource = new CancellationTokenSource();
            _container = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(DisposalUnitUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;

            UpdateInterface();
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponent<AnchorableComponent>();
            var physics = Owner.EnsureComponent<PhysicsComponent>();

            physics.AnchoredChanged += UpdateVisualState;
            UpdateVisualState();
        }

        public override void OnRemove()
        {
            _cancellationTokenSource.Cancel();
            _container = null;

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    var timing = IoCManager.Resolve<IGameTiming>();
                    if (!msg.Entity.HasComponent<HandsComponent>() ||
                        timing.CurTime < _lastExitAttempt + ExitAttemptDelay)
                    {
                        break;
                    }

                    _lastExitAttempt = timing.CurTime;
                    Remove(msg.Entity);
                    break;
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return false;
            }

            if (!eventArgs.User.HasComponent<IHandsComponent>())
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                    Loc.GetString("You have no hands!"));
                return false;
            }

            _userInterface.Open(actor.playerSession);
            return true;
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsert(eventArgs.Using);
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Jump inside");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryInsert(user);
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Flush");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryFlush();
            }
        }
    }
}
