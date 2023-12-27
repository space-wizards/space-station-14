using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Generates dungeon flooring based on the specified noise.
/// </summary>
public sealed partial class NoiseDunGen : IDunGen
{
    // At some point we may want layers masking each other like a simpler version of biome code but for now
    // we'll just make it circular.

    /// <summary>
    /// Radius to spawn the tiles out.
    /// </summary>
    [DataField(required: true)]
    public int Radius;

    [DataField(required: true)]
    public List<NoiseDunGenLayer> Layers = new();
}

[DataRecord]
public record struct NoiseDunGenLayer
{
    /// <summary>
    /// If the noise value is above this then it gets output.
    /// </summary>
    [DataField]
    public float Threshold;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public FastNoiseLite Noise;
}
