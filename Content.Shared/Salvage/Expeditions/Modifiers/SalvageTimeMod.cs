using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageTimeMod")]
public sealed class SalvageTimeMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    [DataField("minDuration")]
    public int MinDuration = 900;

    [DataField("maxDuration")]
    public int MaxDuration = 900;
}
