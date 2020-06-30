

using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.BodySystem {

    class ArmProperty : IExposeData {
        public float ReachDistance;
		
        public void ExposeData(ObjectSerializer serializer){
            serializer.DataField(ref ReachDistance, "reachDistance", 2f);
        }
    }
}
