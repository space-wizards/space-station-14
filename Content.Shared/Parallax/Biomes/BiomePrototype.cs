using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}

[ImplicitDataDefinitionForInheritors]
public interface IBiomeLayer {}

public sealed class BiomeTileLayer : IBiomeLayer
{
    /// <summary>
    /// Threshold for this tile to be present. If set to 0 forces it for every tile.
    /// </summary>
    [DataField("threshold")]
    public float Threshold = 0.5f;

    /// <summary>
    /// Frequency for noise: lower values create larger blobs.
    /// </summary>
    [DataField("frequency")]
    public float Frequency = 0.1f;

    [DataField("tiles", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> Tiles = new();
}

public sealed class BiomeDecalLayer : IBiomeLayer
{
    /// <summary>
    /// What biome tiles we're allowed to draw on. If no biome tile is present (i.e. a real tile is there) nothing drawn.
    /// </summary>
    [DataField("allowedTiles", customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))] public List<string> AllowedTiles = new();

    [DataField("divisions")]
    public float Divisions = 1f;

    [DataField("seedOffset")]
    public int SeedOffset = 0;

    /// <summary>
    /// Frequency for noise: lower values create larger blobs.
    /// </summary>
    [DataField("frequency")]
    public float Frequency = 0.25f;

    /// <summary>
    /// Decals will only be drawn if the noise is above this threshold.
    /// </summary>
    [DataField("threshold")]
    public float Threshold = 0.8f;

    [DataField("decals", required: true)]
    public List<SpriteSpecifier> Decals = new();
}
