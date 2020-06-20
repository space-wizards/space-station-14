using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.BodySystem
{

    /// <summary>
    ///     Used by to determine which callback is used by the server after an option is selected.
    /// </summary>	   
    public enum SurgeryUIMessageType { SelectBodyPart, SelectMechanism }

    [Serializable, NetSerializable]
    public class OpenSurgeryUIMessage : ComponentMessage
    {
        public SurgeryUIMessageType MessageType;
        public OpenSurgeryUIMessage(SurgeryUIMessageType messageType)
        {
            MessageType = messageType;
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class CloseSurgeryUIMessage : ComponentMessage
    {
        public CloseSurgeryUIMessage()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class UpdateSurgeryUIMessage : ComponentMessage
    {
        public Dictionary<string, object> Targets;
        public UpdateSurgeryUIMessage(Dictionary<string, object> targets)
        {
            Targets = targets;
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class ReceiveSurgeryUIMessage : ComponentMessage
    {
        public object SelectedOptionData;
        public SurgeryUIMessageType MessageType;
        public ReceiveSurgeryUIMessage(object selectedOptionData, SurgeryUIMessageType messageType)
        {
            SelectedOptionData = selectedOptionData;
            MessageType = messageType;
        }
    }
}
