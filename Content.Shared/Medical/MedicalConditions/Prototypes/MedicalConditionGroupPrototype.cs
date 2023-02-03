using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.MedicalConditions.Prototypes;

[Prototype("medicalConditionGroup")]
public sealed class MedicalConditionGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;
}
