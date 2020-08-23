using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CloningMachineComponent : SharedCloningMachineComponent, IActivate
    {
        private AppearanceComponent _appearance;
        private BoundUserInterface _userInterface;
        private ContainerSlot _bodyContainer;

        private PowerReceiverComponent _powerReceiver;
        private bool Powered => _powerReceiver.Powered;

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(CloningMachineUIKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;
            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();

            //TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            _userInterface.SetState(newState);

            UpdateUserInterface();
        }

        private void UpdateUserInterface()
        {
            if (!Powered)
            {
                return;
            }

            var newState = GetUserInterfaceState();
            _userInterface.SetState(newState);
        }


        private static readonly CloningMachineBoundUserInterfaceState EmptyUIState =
            new CloningMachineBoundUserInterfaceState(new List<EntityUid>(),0, false);

        private CloningMachineBoundUserInterfaceState GetUserInterfaceState()
        {
            return new CloningMachineBoundUserInterfaceState(CloningSystem.scannedUids,0, false);
        }


        public void Activate(ActivateEventArgs eventArgs)
        {
            if (!Powered ||
                !eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }


            _userInterface.Open(actor.playerSession);
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!(obj.Message is UiButtonPressedMessage message))
            {
                return;
            }

            switch (message.Button)
            {
                case UiButton.Clone:

                    /*if (_bodyContainer.ContainedEntity != null)
                    {
                        CloningSystem.AddToScannedUids(_bodyContainer.ContainedEntity.Uid);
                    }*/
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
