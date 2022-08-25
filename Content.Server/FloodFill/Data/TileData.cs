using Content.Shared.Atmos;

namespace Content.Server.FloodFill.Data;

/// <summary>
///     Data struct that describes the entities blocking fill flood on a tile.
/// </summary>
public struct TileData
{
    /// <summary>
    ///     Resistance towards further fill flood propagation.
    ///     You can save different type of tolerance into different indexes of array.
    /// </summary>
    /// <example>
    ///     In explosion system each tolerance element is mapped
    ///     into resistance of different types of explosion.
    /// </example>
    public float[] Tolerance;

    /// <summary>
    ///     From what incoming direction of fill flood will be blocked by tolerance.
    /// </summary>
    public AtmosDirection BlockedDirections = AtmosDirection.Invalid;

    public TileData(float[] tolerance, AtmosDirection blockedDirections)
    {
        Tolerance = tolerance;
        BlockedDirections = blockedDirections;
    }
}
