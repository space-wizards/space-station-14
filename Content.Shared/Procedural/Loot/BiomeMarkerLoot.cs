using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Adds a biome marker layer for dungeon loot.
/// </summary>
public sealed class BiomeMarkerLoot : IDungeonLoot
{
    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<BiomeMarkerLayerPrototype>))]
    public string Prototype = string.Empty;
}
