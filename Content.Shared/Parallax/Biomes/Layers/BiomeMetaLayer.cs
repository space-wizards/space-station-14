using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Layers;

/// <summary>
/// Contains more biome layers recursively via a biome template.
/// Can be used for sub-biomes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BiomeMetaLayer : IBiomeLayer
{
    [DataField]
    public FastNoiseLite Noise { get; private set; } = new(0);

    /// <inheritdoc/>
    [DataField]
    public float Threshold { get; private set; } = -1f;

    /// <inheritdoc/>
    [DataField]
    public bool Invert { get; private set; }

    [DataField]
    public ProtoId<BiomeTemplatePrototype> Template = string.Empty;
}
