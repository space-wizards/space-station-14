using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    
    public class SharedSurgeryToolComponent : Component {

        protected SurgeryToolType _surgeryToolClass;
        protected float _baseOperateTime;
        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _surgeryToolClass, "surgeryToolClass", SurgeryToolType.Incision);
            serializer.DataField(ref _baseOperateTime, "baseOperateTime", 5);
        }
    }

    [Serializable, NetSerializable]
    public class OpenSurgeryUIMessage : ComponentMessage
    {
        public OpenSurgeryUIMessage()
        {
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
    public class SelectSurgeryUIMessage : ComponentMessage
    {
        public string TargetSlot;
        public SelectSurgeryUIMessage(string target)
        {
            TargetSlot = target;
        }
    }




}

