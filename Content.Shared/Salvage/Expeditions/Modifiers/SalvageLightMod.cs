using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageLightMod")]
public sealed class SalvageLightMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    [DataField("color", required: true)] public Color? Color;
}
