using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Adds the prototype as a biome layer.
/// </summary>
public sealed partial class BiomeLoot : IDungeonLoot
{
    [DataField(required: true)]
    public ProtoId<DungeonConfigPrototype> Proto;
}
