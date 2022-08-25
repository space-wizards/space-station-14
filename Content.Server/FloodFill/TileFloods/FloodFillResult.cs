namespace Content.Server.FloodFill.TileFloods;

/// <summary>
///     Results of fill flood. It contains information about all visited
///     grids tiles (<see cref="GridData"/>) and space tiles (<see cref="SpaceData"/>).
///     Visited tiles stores not resulted intensity data, but fill flood algorithm
///     iterations. To get intensity per tile use <see cref="IterationIntensity"/> mapping.
/// </summary>
public sealed class FloodFillResult
{
    /// <summary>
    ///     Total number of tiles that fill flood has visited.
    /// </summary>
    public readonly int Area;

    /// <summary>
    ///     The flood intensity associated with each tile iteration.
    /// </summary>
    public readonly List<float> IterationIntensity;

    /// <summary>
    ///     Flood data for each that is lay on grid.
    ///     Dictionary key is uid of the grid.
    /// </summary>
    public readonly Dictionary<EntityUid, GridTileFlood> GridData;

    /// <summary>
    ///     Flood data for each tile that doesn't lay on grid (space).
    ///     The world position of tiles is based on <see cref="SpaceMatrix"/>.
    /// </summary>
    public readonly SpaceTileFlood? SpaceData;

    /// <summary>
    ///     The coordinate system of all tiles in space data.
    /// </summary>
    public readonly Matrix3 SpaceMatrix;

    public FloodFillResult(int area, List<float> iterationIntensity, SpaceTileFlood? spaceData,
        Dictionary<EntityUid, GridTileFlood> gridData, Matrix3 spaceMatrix)
    {
        Area = area;
        IterationIntensity = iterationIntensity;
        SpaceData = spaceData;
        GridData = gridData;
        SpaceMatrix = spaceMatrix;
    }
}
