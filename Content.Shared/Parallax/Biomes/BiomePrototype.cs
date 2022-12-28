using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("layers", customTypeSerializer:typeof(PrototypeIdListSerializer<BiomeLayerPrototype>))]
    public List<string> Layers = new();

    [DataField("tileGroups", customTypeSerializer: typeof(PrototypeIdListSerializer<BiomeTileGroupPrototype>))]
    public List<string> TileGroups = new();
}

[Prototype("biomeLayer")]
public sealed class BiomeLayerPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;


}

[Prototype("biomeTileGroup")]
public sealed class BiomeTileGroupPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [DataField("tile", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;

    [DataField("weight")]
    public float Weight = 1f;

    /// <summary>
    /// If we draw overlapping edge sprites to other biome groups.
    /// </summary>
    [DataField("edges")]
    public Dictionary<BiomeEdge, ResourcePath> Edges = new();
}

public enum BiomeEdge : byte
{
    None = 0,
    Single,
    Double,
    Triple,
}

