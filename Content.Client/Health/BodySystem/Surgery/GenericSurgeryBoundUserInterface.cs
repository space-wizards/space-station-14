using System;
using System.Collections.Generic;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;
using Content.Shared.BodySystem;

namespace Content.Client.BodySystem
{
    public class GenericSurgeryBoundUserInterface : BoundUserInterface
    {

#pragma warning disable CS0649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore
        


        public GenericSurgeryBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {

        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                
            }
        }

        protected override void Dispose(bool disposing)
        {

        }
    }
}




namespace Content.Client.BodySystem
{
    //TODO : Make window close if target or surgery tool gets too far away from user.

    /// <summary>
    ///     Client-side component representing a generic tool capable of performing surgery. This client version exclusively handles UI popups.
    /// </summary>
    [RegisterComponent]
    public class ClientSurgeryToolComponent : SharedSurgeryToolComponent
    {
        private GenericSurgeryWindow _window;
        private SurgeryUIMessageType _currentDisplayType;

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case OpenSurgeryUIMessage msg:
                    HandleOpenSurgeryUIMessage(msg);
                    break;
                case CloseSurgeryUIMessage msg:
                    HandleCloseSurgeryUIMessage();
                    break;
                case UpdateSurgeryUIMessage msg:
                    HandleUpdateSurgeryUIMessage(msg);
                    break;
            }
        }
        public override void OnAdd()
        {
            base.OnAdd();
            _window = new GenericSurgeryWindow(OptionSelectedCallback, CloseCallback);
        }
        public override void OnRemove()
        {
            _window.Dispose();
            base.OnRemove();
        }

        private void HandleOpenSurgeryUIMessage(OpenSurgeryUIMessage openSurgeryUIMessage)
        {
            _currentDisplayType = openSurgeryUIMessage.MessageType;
            _window.OpenCentered();
        }
        private void HandleCloseSurgeryUIMessage()
        {
            _window.CloseNoCallback();
        }
        private void HandleUpdateSurgeryUIMessage(UpdateSurgeryUIMessage updateSurgeryUIMessage)
        {
            _window.BuildDisplay(updateSurgeryUIMessage.Targets);
        }

        private void CloseCallback()
        {
            SendNetworkMessage(new CloseSurgeryUIMessage());
        }
        private void OptionSelectedCallback(object selectedOptionData)
        {
            SendNetworkMessage(new ReceiveSurgeryUIMessage(selectedOptionData, _currentDisplayType));
        }

    }
}
