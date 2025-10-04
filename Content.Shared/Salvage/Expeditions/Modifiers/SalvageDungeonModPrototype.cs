using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype]
public sealed partial class SalvageDungeonModPrototype : IPrototype, IBiomeSpecificMod
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("desc")] public LocId Description { get; private set; } = string.Empty;

    /// <inheridoc/>
    [DataField("cost")]
    public float Cost { get; private set; } = 0f;

    /// <inheridoc/>
    [DataField]
    public List<ProtoId<SalvageBiomeModPrototype>>? Biomes { get; private set; } = null;

    /// <summary>
    /// Which factions can spawn on this dungeon; any if empty.
    /// </summary>
    [DataField("factions")]
    public List<ProtoId<SalvageFactionPrototype>>? Factions { get; private set; } = null;

    /// <summary>
    /// The config to use for spawning the dungeon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DungeonConfigPrototype> Proto = string.Empty;
}
