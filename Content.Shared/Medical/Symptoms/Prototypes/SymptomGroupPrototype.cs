using System.Collections.Immutable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Symptoms.Prototypes;

[Prototype("symptomGroup")]
public sealed class SymptomGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("symptoms", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public readonly ImmutableHashSet<string> Symptoms = ImmutableHashSet<string>.Empty;
}
