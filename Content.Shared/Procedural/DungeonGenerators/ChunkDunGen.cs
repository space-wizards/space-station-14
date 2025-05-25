using Robust.Shared.Noise;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Turns a chunked area into a dungeon for layer purposes. Assumes the position is the BL origin.
/// </summary>
public sealed partial class ChunkDunGen : IDunGenLayer
{
    [DataField]
    public int Size = 16;

    /// <summary>
    /// Noise to apply for each tile conditionally.
    /// </summary>
    [DataField]
    public FastNoiseLite? Noise;

    /// <summary>
    /// Threshold for noise. Does nothing if <see cref="Noise"/> is null.
    /// </summary>
    [DataField]
    public float Threshold = -1f;
}
