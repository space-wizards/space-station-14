using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Adds a biome marker layer for dungeon loot.
/// </summary>
public sealed partial class BiomeMarkerLoot : IDungeonLoot
{
    [DataField("proto", required: true,
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<string, BiomeMarkerLayerPrototype>))]
    public Dictionary<string, string> Prototype = new();
}
