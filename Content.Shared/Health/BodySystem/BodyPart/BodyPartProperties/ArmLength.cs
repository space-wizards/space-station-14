using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.Health.BodySystem.BodyPart.BodyPartProperties {

    [NetSerializable, Serializable]
    class ArmLength : IExposeData {
        private float _length;

        public void ExposeData(ObjectSerializer serializer){
            serializer.DataField(ref _length, "length", 2f);
        }
    }
}
