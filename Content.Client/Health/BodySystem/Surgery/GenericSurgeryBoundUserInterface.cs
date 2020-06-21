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

    //TODO : Make window close if target or surgery tool gets too far away from user.

    /// <summary>
    ///     Generic client-side UI list popup that allows users to choose from an option of limbs or organs to operate on.
    /// </summary>
    public class GenericSurgeryBoundUserInterface : BoundUserInterface
    {

        private GenericSurgeryWindow _window;
        private SurgeryUIMessageType _currentDisplayType;

        public GenericSurgeryBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            _window = new GenericSurgeryWindow(OptionSelectedCallback);
            _window.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case UpdateSurgeryUIMessage msg:
                    HandleUpdateSurgeryUIMessage(msg);
                    break;
            }
        }

        private void HandleUpdateSurgeryUIMessage(UpdateSurgeryUIMessage updateSurgeryUIMessage)
        {
            _currentDisplayType = updateSurgeryUIMessage.MessageType;
            _window.BuildDisplay(updateSurgeryUIMessage.Targets);
        }

        private void OptionSelectedCallback(object selectedOptionData)
        {
            SendMessage(new ReceiveSurgeryUIMessage(selectedOptionData, _currentDisplayType));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _window.Dispose();
        }
    }
}
