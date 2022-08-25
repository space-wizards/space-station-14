using Content.Shared.Atmos;

namespace Content.Server.FloodFill.Data;

/// <summary>
///     This class has information about the space equivalent of an entity blocking grid flood: the edges of grids.
/// </summary>
public sealed class BlockedSpaceTile
{
    /// <summary>
    ///     What directions of this tile are not blocked?
    /// </summary>
    public AtmosDirection UnblockedDirections = AtmosDirection.All;

    /// <summary>
    ///     The set of grid edge-tiles that are blocking this space tile.
    /// </summary>
    public readonly List<GridEdgeData> BlockingGridEdges = new();

    public sealed class GridEdgeData
    {
        public Vector2i Tile;
        public EntityUid? Grid;
        public Box2Rotated Box;

        public GridEdgeData(Vector2i tile, EntityUid? grid, Vector2 center, Angle angle, float size)
        {
            Tile = tile;
            Grid = grid;
            Box = new(Box2.CenteredAround(center, (size, size)), angle, center);
        }
    }
}
