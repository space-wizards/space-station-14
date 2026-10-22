using Robust.Shared.Noise;

namespace Content.Shared.Parallax.Biomes.Layers;

[ImplicitDataDefinitionForInheritors]
public partial interface IBiomeLayer
{
    /// <summary>
    /// Seed is used an offset from the relevant BiomeComponent's seed.
    /// </summary>
    FastNoiseLite Noise { get; }

    /// <summary>
    /// Threshold for this layer to be present. If set to 0 forces it for every tile.
    /// </summary>
    float Threshold { get; }

    /// <summary>
    /// Is the thresold inverted so we need to be lower than it.
    /// </summary>
    public bool Invert { get; }
}
