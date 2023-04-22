using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Layers;

[Serializable, NetSerializable]
public sealed class  BiomeTileLayer : IBiomeLayer
{
    [DataField("noise")] public FastNoiseLite Noise { get; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    /// <inheritdoc/>
    [DataField("invert")] public bool Invert { get; } = false;

    /// <summary>
    /// Which tile variants to use for this layer. Uses all of the tile's variants if none specified
    /// </summary>
    [DataField("variants")]
    public List<byte>? Variants = null;

    [DataField("tile", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;
}
