using Robust.Shared.Noise;

namespace Content.Shared.Parallax.Biomes.Layers;

/// <summary>
/// Dummy layer that specifies a marker to be replaced by external code.
/// For example if they wish to add their own layers.
/// </summary>
public sealed class BiomeMarkerLayer : IBiomeLayer
{
    /*
     * This exists so we can just store the biome prototype on the component and call it a day.
     * The alternative is we load biome layers onto the component and then do things to handle prototype reloads.
     */
    public FastNoiseLite Noise { get; } = new();
    public float Threshold { get; }
    public bool Invert { get; }
}
