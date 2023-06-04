using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Parallax.Biomes.Layers;

[Serializable, NetSerializable]
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

    /// <inheritdoc/>
    [DataField("invert")] public bool Invert { get; } = false;

    [DataField("decals", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<DecalPrototype>))]
    public List<string> Decals = new();
}
