using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("injury")]
public sealed class InjuryPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("allowStacking")] public bool AllowStacking { get; init; } = true;

    [DataField("name", required: true)] public string DisplayName { get; init; } = string.Empty;

    [DataField("description", required: true)]
    public string Description { get; init; } = string.Empty;

    [DataField("pain", required: true)] public float Pain { get; init; }

    [DataField("bleedRate", required: false)]
    public float Bleed { get; init; }
}

public static class A
{
    // IT LIVES ON! FOREVER IN OUR HEARTS!
}
