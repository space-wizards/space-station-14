using Content.Shared.Decals;
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
    [IdDataField] public string ID { get; } = default!;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}

[ImplicitDataDefinitionForInheritors]
public interface IBiomeLayer
{
    /// <summary>
    /// Threshold for this layer to be present. If set to 0 forces it for every tile.
    /// </summary>
    float Threshold { get; }

    /// <summary>
    /// Offset the seed by the specified amount for this layer.
    /// Useful if you have 2 similar layers but don't want them to match exactly.
    /// </summary>
    int SeedOffset { get; }

    /// <summary>
    /// Frequency for noise: lower values create larger blobs.
    /// </summary>
    float Frequency { get; }
}

public sealed class BiomeTileLayer : IBiomeLayer
{
    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    /// <inheritdoc/>
    [DataField("seedOffset")]
    public int SeedOffset { get; } = 0;

    /// <inheritdoc/>
    [DataField("frequency")]
    public float Frequency { get; } = 0.1f;

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

    /// <inheritdoc/>
    [DataField("seedOffset")]
    public int SeedOffset { get; } = 0;

    /// <inheritdoc/>
    [DataField("frequency")]
    public float Frequency { get; } = 0.25f;

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

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    /// <inheritdoc/>
    [DataField("seedOffset")]
    public int SeedOffset { get; } = 0;

    /// <inheritdoc/>
    [DataField("frequency")]
    public float Frequency { get; } = 0.1f;

    [DataField("entities", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Entities = new();
}
