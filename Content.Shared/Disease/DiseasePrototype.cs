using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Disease
{
    [Prototype("disease")]
    [DataDefinition]
    public sealed class DiseasePrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("parent", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        public string? Parent { get; private set; }

        [NeverPushInheritance]
        [DataField("abstract")]
        public bool Abstract { get; private set; }

        public float Accumulator = 0f;
        [DataField("effects", serverOnly: true)]
        public readonly List<DiseaseEffect> Effects = new(0);

        [DataField("cures", serverOnly: true)]
        public readonly List<DiseaseCure> Cures = new(0);

        [DataField("cureResist", serverOnly: true)]
        public float CureResist = 0.05f;

        [DataField("infectious", serverOnly: true)]
        public bool Infectious = true;

    }
}
