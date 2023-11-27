using Robust.Shared.Prototypes;

namespace Content.Shared.Parallax.Biomes.Markers;

/// <summary>
/// Specifies one-off marker points to be used. This could be for dungeon markers, mob markers, etc.
/// These are run outside of the tile / decal / entity layers.
/// </summary>
public interface IBiomeMarkerLayer : IPrototype
{
    /// <summary>
    /// Biome template to use as a mask for this layer.
    /// </summary>
    public Dictionary<EntProtoId, EntProtoId> EntityMask { get; }

    public string? Prototype { get; }

    /// <summary>
    /// How large the pre-generated points area is.
    /// </summary>
    public int Size { get; }
}
