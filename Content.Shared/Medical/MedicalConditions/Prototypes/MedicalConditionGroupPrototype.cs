using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.MedicalConditions.Prototypes;

[Prototype("medicalConditionCategory")]
public sealed class MedicalConditionGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;
}
