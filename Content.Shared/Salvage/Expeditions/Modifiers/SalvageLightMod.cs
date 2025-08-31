using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageLightMod")]
public sealed partial class SalvageLightMod : IPrototype, IBiomeSpecificMod
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("desc")] public LocId Description { get; private set; } = string.Empty;

    /// <inheritdoc/>
    [DataField("cost")]
    public float Cost { get; private set; } = 0f;

    /// <inheritdoc/>
    [DataField]
    public List<ProtoId<SalvageBiomeModPrototype>>? Biomes { get; private set; } = null;

    // 🌟Starlight🌟
    [DataField]
    public List<ProtoId<SalvageDifficultyPrototype>>? Difficulties { get; private set; } = null;

    [DataField("color", required: true)] public Color? Color;
}
