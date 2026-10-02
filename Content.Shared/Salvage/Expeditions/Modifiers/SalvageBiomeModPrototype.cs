using Content.Shared.Parallax.Biomes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

/// <summary>
/// Affects the biome to be used for salvage.
/// </summary>
[Prototype]
public sealed partial class SalvageBiomeModPrototype : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("desc", required: true)]
    public LocId Description { get; private set; }

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField]
    public float Cost { get; private set; } = 0f;

    /// <summary>
    /// Is weather allowed to apply to this biome.
    /// </summary>
    [DataField]
    public bool Weather = true;

    [DataField("biome", required: true)]
    public ProtoId<BiomeTemplatePrototype>? BiomePrototype;
}
