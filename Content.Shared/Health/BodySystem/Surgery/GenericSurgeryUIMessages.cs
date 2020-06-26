using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.BodySystem
{

    [Serializable, NetSerializable]
    public class RequestBodyPartSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public Dictionary<string, int> Targets;
        public RequestBodyPartSurgeryUIMessage(Dictionary<string, int> targets)
        {
            Targets = targets;
        }
    }
    [Serializable, NetSerializable]
    public class RequestMechanismSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public Dictionary<string, int> Targets;
        public RequestMechanismSurgeryUIMessage(Dictionary<string, int> targets)
        {
            Targets = targets;
        }
    }
    [Serializable, NetSerializable]
    public class RequestBodyPartSlotSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public Dictionary<string, int> Targets;
        public RequestBodyPartSlotSurgeryUIMessage(Dictionary<string, int> targets)
        {
            Targets = targets;
        }
    }





    [Serializable, NetSerializable]
    public class ReceiveBodyPartSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public int SelectedOptionID;
        public ReceiveBodyPartSurgeryUIMessage(int selectedOptionID)
        {
            SelectedOptionID = selectedOptionID;
        }
    }
    [Serializable, NetSerializable]
    public class ReceiveMechanismSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public int SelectedOptionID;
        public ReceiveMechanismSurgeryUIMessage(int selectedOptionID)
        {
            SelectedOptionID = selectedOptionID;
        }
    }
    [Serializable, NetSerializable]
    public class ReceiveBodyPartSlotSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public int SelectedOptionID;
        public ReceiveBodyPartSlotSurgeryUIMessage(int selectedOptionID)
        {
            SelectedOptionID = selectedOptionID;
        }
    }




    [NetSerializable, Serializable]
    public enum GenericSurgeryUiKey
    {
        Key,
    }
}
