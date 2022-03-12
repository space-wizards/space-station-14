using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    public sealed class DiseaseGiverComponent : Component
    {
        [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Disease = string.Empty;

        [DataField("doAfterLength")]
        public float DoAfterLength = 0f;
    }
}
