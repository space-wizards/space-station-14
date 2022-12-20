using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("tiles", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, ContentTileDefinition>))]
    public Dictionary<string, float> Tiles = new();
}
