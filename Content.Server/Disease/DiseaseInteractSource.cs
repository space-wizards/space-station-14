using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease
{
    [RegisterComponent]
    public sealed class DiseaseInteractSourceComponent : Component
    {
        [DataField("diseases", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public DiseasePrototype Disease = default!;
    }
}
