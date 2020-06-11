using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Storage;
using Content.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Content.Shared.BodySystem;
using System.Globalization;

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
            _window.Close();
        }
        private void HandleUpdateSurgeryUIMessage(UpdateSurgeryUIMessage updateSurgeryUIMessage)
        {
            _window.BuildDisplay(updateSurgeryUIMessage.Targets);
        }

        private void CloseCallback()
        {
            SendNetworkMessage(new CloseSurgeryUIMessage());
        }
        private void OptionSelectedCallback(string selectedOptionData)
        {
            SendNetworkMessage(new ReceiveSurgeryUIMessage(selectedOptionData, _currentDisplayType));
        }
   
    }
}
