using Content.Shared.Parallax.Biomes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Adds a biome template layer for dungeon loot.
/// </summary>
public sealed partial class BiomeTemplateLoot : IDungeonLoot
{
    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<BiomeTemplatePrototype>))]
    public string Prototype = string.Empty;
}
