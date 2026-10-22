using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Adds a biome marker layer for dungeon loot.
/// </summary>
public sealed partial class BiomeMarkerLoot : IDungeonLoot
{
    [DataField("proto", required: true)]
    public ProtoId<BiomeMarkerLayerPrototype> Prototype = new();
}
