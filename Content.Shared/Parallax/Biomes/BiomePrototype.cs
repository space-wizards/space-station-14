using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : BiomeLayerPrototype
{
    [DataField("layers", customTypeSerializer:typeof(PrototypeIdListSerializer<BiomeLayerPrototype>))]
    public List<string> Layers = new();
}

public abstract class BiomeLayerPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;
}

[Prototype("biomeTileLayer")]
public sealed class BiomeTileLayer : BiomeLayerPrototype
{
    [DataField("tiles", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public List<string> Tiles = new();
}
