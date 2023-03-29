using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")]
    public string Description = string.Empty;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}

[ImplicitDataDefinitionForInheritors]
public interface IBiomeLayer
{
    /// <summary>
    /// Seed is used an offset from the relevant BiomeComponent's seed.
    /// </summary>
    FastNoiseLite Noise { get; }

    /// <summary>
    /// Threshold for this layer to be present. If set to 0 forces it for every tile.
    /// </summary>
    float Threshold { get; }
}

public sealed class BiomeTileLayer : IBiomeLayer
{
    [DataField("noise")] public FastNoiseLite Noise { get; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    /// <summary>
    /// Which tile variants to use for this layer. Uses all of the tile's variants if none specified
    /// </summary>
    [DataField("variants")]
    public List<byte>? Variants = null;

    [DataField("tile", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;
}

/// <summary>
/// Handles actual objects such as decals and entities.
/// </summary>
public interface IBiomeWorldLayer : IBiomeLayer
{
    /// <summary>
    /// What tiles we're allowed to spawn on, real or biome.
    /// </summary>
    List<string> AllowedTiles { get; }
}

public sealed class BiomeDecalLayer : IBiomeWorldLayer
{
    /// <inheritdoc/>
    [DataField("allowedTiles", customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> AllowedTiles { get; } = new();

    /// <summary>
    /// Divide each tile up by this amount.
    /// </summary>
    [DataField("divisions")]
    public float Divisions = 1f;

    [DataField("noise")]
    public FastNoiseLite Noise { get; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.8f;

    [DataField("decals", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<DecalPrototype>))]
    public List<string> Decals = new();
}

public sealed class BiomeEntityLayer : IBiomeWorldLayer
{
    /// <inheritdoc/>
    [DataField("allowedTiles", customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> AllowedTiles { get; } = new();

    [DataField("noise")] public FastNoiseLite Noise { get; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    [DataField("entities", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Entities = new();
}
