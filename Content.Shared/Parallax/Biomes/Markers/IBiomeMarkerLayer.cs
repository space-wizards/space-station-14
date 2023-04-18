using Robust.Shared.Prototypes;

namespace Content.Shared.Parallax.Biomes.Points;

/// <summary>
/// Specifies one-off marker points to be used. This could be for dungeon markers, mob markers, etc.
/// These are run outside of the tile / decal / entity layers.
/// </summary>
public interface IBiomeMarkerLayer : IPrototype
{
    /// <summary>
    /// Pre-generate points this many sets of points.
    /// </summary>
    [DataField("variations")]
    public int Variations { get; }

    /// <summary>
    /// Minimum radius between 2 points
    /// </summary>
    [DataField("radius")]
    public float Radius { get; }

    /// <summary>
    /// How many points to insert into the area.
    /// </summary>
    [DataField("count")]
    public int Count { get; }

    /// <summary>
    /// How large the pre-generated points area is.
    /// </summary>
    [DataField("size")]
    public int Size { get; }
}
