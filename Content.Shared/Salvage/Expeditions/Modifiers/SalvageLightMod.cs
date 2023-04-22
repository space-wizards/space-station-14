using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageLightMod")]
public sealed class SalvageLightMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    [DataField("color", required: true)] public Color? Color;

    /// <summary>
    /// Biomes that this color applies to.
    /// </summary>
    [DataField("biomes", customTypeSerializer: typeof(PrototypeIdListSerializer<SalvageBiomeMod>))]
    public List<string>? Biomes;
}
