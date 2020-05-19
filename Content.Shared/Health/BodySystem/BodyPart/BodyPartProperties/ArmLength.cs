

using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.BodySystem {

    [NetSerializable, Serializable]
    class ArmLength : IExposeData {
        private float _length;
		
        public void ExposeData(ObjectSerializer serializer){
            serializer.DataField(ref _length, "length", 2f);
        }
    }
}
