using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Symptoms.Prototypes;

[Prototype("symptomGroup")]
public sealed class SymptomGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;
}
