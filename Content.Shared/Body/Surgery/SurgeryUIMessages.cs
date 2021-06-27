#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery
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
        public int SelectedOptionId;

        public ReceiveBodyPartSurgeryUIMessage(int selectedOptionId)
        {
            SelectedOptionId = selectedOptionId;
        }
    }

    [Serializable, NetSerializable]
    public class ReceiveMechanismSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public int SelectedOptionId;

        public ReceiveMechanismSurgeryUIMessage(int selectedOptionId)
        {
            SelectedOptionId = selectedOptionId;
        }
    }

    [Serializable, NetSerializable]
    public class ReceiveBodyPartSlotSurgeryUIMessage : BoundUserInterfaceMessage
    {
        public int SelectedOptionId;

        public ReceiveBodyPartSlotSurgeryUIMessage(int selectedOptionId)
        {
            SelectedOptionId = selectedOptionId;
        }
    }
}
