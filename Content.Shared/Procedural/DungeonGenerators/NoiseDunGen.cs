using Content.Shared.Procedural.Distance;
using Robust.Shared.Noise;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Generates dungeon flooring based on the specified noise.
/// </summary>
public sealed partial class NoiseDunGen : IDunGenLayer
{
    /*
     * Floodfills out from 0 until it finds a valid tile.
     * From here it then floodfills until it can no longer fill in an area and generates a dungeon from that.
     */

    // At some point we may want layers masking each other like a simpler version of biome code but for now
    // we'll just make it circular.

    /// <summary>
    /// How many areas of noise to fill out. Useful if we just want 1 blob area to fill out.
    /// </summary>
    [DataField]
    public int Iterations = int.MaxValue;

    /// <summary>
    /// Cap on how many tiles to include.
    /// </summary>
    [DataField]
    public int TileCap = 128;

    /// <summary>
    /// Standard deviation of tilecap.
    /// </summary>
    [DataField]
    public float CapStd = 8f;

    [DataField(required: true)]
    public List<NoiseDunGenLayer> Layers = new();
}

[DataRecord]
public partial record struct NoiseDunGenLayer
{
    /// <summary>
    /// If the noise value is above this then it gets output.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public string Tile;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
