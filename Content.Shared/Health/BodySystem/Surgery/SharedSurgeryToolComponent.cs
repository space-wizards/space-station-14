using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.BodySystem {

    
    public abstract class SharedSurgeryToolComponent : Component {
        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;
    }

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
        public Dictionary<string, string> Targets;
        public UpdateSurgeryUIMessage(Dictionary<string, string> targets)
        {
            Targets = targets;
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class ReceiveSurgeryUIMessage : ComponentMessage
    {
        public string SelectedOptionData;
        public SurgeryUIMessageType MessageType;
        public ReceiveSurgeryUIMessage(string selectedOptionData, SurgeryUIMessageType messageType)
        {
            SelectedOptionData = selectedOptionData;
            MessageType = messageType;
        }
    }




}

