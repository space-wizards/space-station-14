using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Parallax.Biomes.Layers;

/// <summary>
/// Contains more biome layers recursively via a biome template.
/// Can be used for sub-biomes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BiomeMetaLayer : IBiomeLayer
{
    [DataField("noise")]
    public FastNoiseLite Noise { get; private set; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; private set; } = -1f;

    /// <inheritdoc/>
    [DataField("invert")]
    public bool Invert { get; private set; }

    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Template = string.Empty;
}
