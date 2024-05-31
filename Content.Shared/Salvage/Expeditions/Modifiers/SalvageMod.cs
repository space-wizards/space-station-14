using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

/// <summary>
/// Generic modifiers with no additional data
/// </summary>
[Prototype("salvageMod")]
public sealed partial class SalvageMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; private set; } = 0f;
}
