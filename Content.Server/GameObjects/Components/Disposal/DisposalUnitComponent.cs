#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.Components.Disposal.DisposalUnit;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using System;
using static Content.Server.GameObjects.Components.Disposal.DisposalInserterComponent;
using static Content.Shared.GameObjects.Components.Disposal.DisposalUnit.DisposalUnitBoundUserInterfaceState;
using static Content.Shared.GameObjects.Components.Disposal.UiButtonPressedMessage;

namespace Content.Server.GameObjects.Components.Disposal
{
    //TODO: Documenting
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent, IInteractHand, IActivate
    {
        [ComponentDependency]
        public readonly PowerReceiverComponent? PowerReceiver = null;

        [ComponentDependency]
        public readonly IPhysicsComponent? Physics = null;

        [ComponentDependency]
        public readonly DisposalInserterComponent? Inserter = null;

        [ComponentDependency]
        public readonly AppearanceComponent? Appearance = null;

        [ViewVariables]
        public bool Anchored => Physics != null && Physics.Anchored;

        [ViewVariables]
        public bool Powered => PowerReceiver != null && PowerReceiver.Powered;

        [ViewVariables]
        public bool ContainsEntities => Inserter != null && Inserter.ContainedEntities.Count > 0;

        [ViewVariables]
        protected BoundUserInterface? UserInterface;

        [ViewVariables]
        protected PressureState State = PressureState.Pressurizing;

        [ViewVariables]
        protected bool Engaged = false;

        private float _pressure = 0f;

        private float _targetPressure = 0f;

        private bool _flushed = false;

        public override void Initialize()
        {
            base.Initialize();

            UserInterface = Owner.GetUIOrNull(DisposalUnitUiKey.Key);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateInterface();
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            UserInterface?.CloseAll();
            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PowerChangedMessage:
                    UpdateInterface();
                    UpdateVisualState();
                    break;
                case AnchoredChangedMessage:

                    if(!Anchored)
                    {
                        UserInterface?.CloseAll();
                    }

                    UpdateVisualState();
                    break;
                case PressureChangedMessage pressureChanged:
                    PressureChanged(pressureChanged);
                    break;
                case InserterFlushedMessage flush:
                    Engaged = false;
                    UpdateVisualState(!flush.Failed);
                    _flushed = !flush.Failed;
                    UpdateInterface();
                    break;
                case EntityInputComponent.EntityInsertetMessage insertedMessage:
                    AfterInsert(insertedMessage.Entity);
                    break;
            }
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateVisualState();
        }

        protected static bool PlayerCanUse(IEntity? player)
        {
            if (player == null)
            {
                return false;
            }

            if (!ActionBlockerSystem.CanInteract(player) || !ActionBlockerSystem.CanUse(player))
            {
                return false;
            }

            return true;
        }

        protected virtual void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null || !PlayerCanUse(obj.Session.AttachedEntity))
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
                    SendMessage(new EntityInputComponent.EjectInputContentsMessage());
                    UpdateVisualState();
                    break;
                case UiButton.Engage:
                    ToggleEngage();
                    break;
                case UiButton.Power:
                    TogglePower();
                    EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual bool OpenUiForUser(ITargetedInteractEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }

            if (IsValidInteraction(eventArgs))
            {
                UserInterface?.Open(actor.playerSession);
                UserInterface?.SendMessage(new DisposalUnitPressureChangedMessage(_pressure, _targetPressure));
                return true;
            }

            return false;
        }

        protected virtual void UpdateInterface()
        {
            var state = new DisposalUnitBoundUserInterfaceState(Owner.Name, State, Powered, Engaged);
            UserInterface?.SetState(state);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return OpenUiForUser(eventArgs);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            OpenUiForUser(eventArgs);
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

            if (!eventArgs.User.HasComponent<IHandsComponent>())
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands!"));
                return false;
            }

            return true;
        }

        private void PressureChanged(PressureChangedMessage pressureChanged)
        {
            //This is neccesarry so that the apprearance component has eneough time to receive the flushing visual state
            if(_flushed)
            {
                _flushed = false;
                return;
            }

            _pressure = pressureChanged.Pressure;
            _targetPressure = pressureChanged.TargetPressure;

            var oldState = State;
            State = _pressure >= _targetPressure ? PressureState.Ready : PressureState.Pressurizing;
            if (oldState != State)
            {
                UpdateInterface();
                UpdateVisualState();
            }

            UserInterface?.SendMessage(new DisposalUnitPressureChangedMessage(_pressure, _targetPressure));
        }

        private void AfterInsert(IEntity entity)
        {
            if (entity.TryGetComponent(out IActorComponent? actor))
            {
                UserInterface?.Close(actor.playerSession);
            }

            UpdateVisualState();
        }

        private void TogglePower()
        {
            if(PowerReceiver != null)
            {
                PowerReceiver.PowerDisabled = !PowerReceiver.PowerDisabled;
            }

            UpdateInterface();
        }

        private void ToggleEngage()
        {
            Engaged ^= true;
            SendMessage(new EngageInserterMessage(Engaged));
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            UpdateVisualState(false);
        }

        private void UpdateVisualState(bool flush)
        {
            if (Appearance == null)
            {
                return;
            }

            if (!Anchored)
            {
                Appearance.SetData(Visuals.VisualState, VisualState.UnAnchored);
                Appearance.SetData(Visuals.Handle, HandleState.Normal);
                Appearance.SetData(Visuals.Light, LightState.Off);
                return;
            }
            else if (flush)
            {
                Appearance.SetData(Visuals.VisualState, VisualState.Flushing);
                Appearance.SetData(Visuals.Light, LightState.Off);
                Appearance.SetData(Visuals.Handle, Engaged ? HandleState.Engaged : HandleState.Normal);
                return;
            }
            else if (State == PressureState.Pressurizing)
            {
                Appearance.SetData(Visuals.VisualState, VisualState.Charging);
            }
            else
            {
                Appearance.SetData(Visuals.VisualState, VisualState.Anchored);
            }

            Appearance.SetData(Visuals.Handle, Engaged ? HandleState.Engaged : HandleState.Normal);

            if (!Powered)
            {
                Appearance.SetData(Visuals.Light, LightState.Off);
                return;
            }

            if (ContainsEntities && State != PressureState.Pressurizing)
            {
                Appearance.SetData(Visuals.Light, LightState.Full);
                return;
            }

            Appearance.SetData(Visuals.Light, State == PressureState.Pressurizing ? LightState.Charging : LightState.Ready);
        }
    }
}
