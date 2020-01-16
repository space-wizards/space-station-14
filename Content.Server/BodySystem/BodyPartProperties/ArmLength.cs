

using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Robust.Shared.BodySystem {
    class ArmLength : IExposeData {
		private float _length;
		
		public void ExposeData(ObjectSerializer serializer){
            serializer.DataField(ref _length, "length", 2f);
		}
	}
}
