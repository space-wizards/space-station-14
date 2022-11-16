using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("injury")]
public sealed class InjuryPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("allowStacking")]
    public readonly bool AllowStacking = true;

    [DataField("name", required: true)]
    public readonly string DisplayName = string.Empty;

    [DataField("description", required: true)]
    public readonly string Description = string.Empty;

    [DataField("pain", required: true)]
    public readonly float Pain;

    [DataField("bleedRate", required: false)]
    public readonly float Bleed;

    [DataField("healthDamage")]
    public readonly FixedPoint2 HealthDamage;

    [DataField("integrityDamage")]
    public readonly FixedPoint2 IntegrityDamage;
}

public static class A
{
    // IT LIVES ON! FOREVER IN OUR HEARTS!
}
