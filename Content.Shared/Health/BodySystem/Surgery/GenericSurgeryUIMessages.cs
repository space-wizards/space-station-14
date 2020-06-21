using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.BodySystem
{

    /// <summary>
    ///     Used to determine which callback is used by the server after an option is selected.
    /// </summary>	   
    public enum SurgeryUIMessageType { SelectBodyPart, SelectMechanism, SelectBodyPartSlot }

    [Serializable, NetSerializable]
    public class UpdateSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public SurgeryUIMessageType MessageType;
        public Dictionary<string, object> Targets;
        public UpdateSurgeryUIMessage(SurgeryUIMessageType messageType, Dictionary<string, object> targets)
        {
            MessageType = messageType;
            Targets = targets;
        }
    }

    [Serializable, NetSerializable]
    public class ReceiveSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public object SelectedOptionData;
        public SurgeryUIMessageType MessageType;
        public ReceiveSurgeryUIMessage(object selectedOptionData, SurgeryUIMessageType messageType)
        {
            SelectedOptionData = selectedOptionData;
            MessageType = messageType;
        }
    }

    [NetSerializable, Serializable]
    public enum GenericSurgeryUiKey
    {
        Key,
    }
}
