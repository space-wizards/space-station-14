using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Parallax.Biomes.Layers;

[Serializable, NetSerializable]
public sealed partial class BiomeTileLayer : IBiomeLayer
{
    [DataField("noise")] public FastNoiseLite Noise { get; private set; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; private set; } = 0.5f;

    /// <inheritdoc/>
    [DataField("invert")] public bool Invert { get; private set; } = false;

    /// <summary>
    /// Which tile variants to use for this layer. Uses all of the tile's variants if none specified
    /// </summary>
    [DataField("variants")]
    public List<byte>? Variants = null;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;
}
