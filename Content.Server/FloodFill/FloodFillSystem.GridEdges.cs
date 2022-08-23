using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.FloodFill;

public partial class FloodFillSystem
{
    // array indices match NeighborFlags shifts.
    public static readonly Vector2i[] NeighbourVectors =
    {
        new (0, 1),
        new (1, 1),
        new (1, 0),
        new (1, -1),
        new (0, -1),
        new (-1, -1),
        new (-1, 0),
        new (-1, 1)
    };

    public static bool AnyNeighborBlocked(NeighborFlag neighbors, AtmosDirection blockedDirs)
    {
        if ((neighbors & NeighborFlag.North) == NeighborFlag.North && (blockedDirs & AtmosDirection.North) == AtmosDirection.North)
            return true;

        if ((neighbors & NeighborFlag.South) == NeighborFlag.South && (blockedDirs & AtmosDirection.South) == AtmosDirection.South)
            return true;

        if ((neighbors & NeighborFlag.East) == NeighborFlag.East && (blockedDirs & AtmosDirection.East) == AtmosDirection.East)
            return true;

        if ((neighbors & NeighborFlag.West) == NeighborFlag.West && (blockedDirs & AtmosDirection.West) == AtmosDirection.West)
            return true;

        return false;
    }

    /// <summary>
    ///     Set of tiles of each grid that are directly adjacent to space, along with the directions that face space.
    /// </summary>
    private Dictionary<EntityUid, Dictionary<Vector2i, NeighborFlag>> _gridEdges = new();

    /// <summary>
    ///     Take our map of grid edges, where each is defined in their own grid's reference frame, and map those
    ///     edges all onto one grids reference frame.
    /// </summary>
    public (Dictionary<Vector2i, BlockedSpaceTile>, ushort) TransformGridEdges(
        MapCoordinates epicentre,
        EntityUid? referenceGrid,
        List<EntityUid> localGrids,
        float maxDistance)
    {
        Dictionary<Vector2i, BlockedSpaceTile> transformedEdges = new();

        var targetMatrix = Matrix3.Identity;
        Angle targetAngle = new();
        var tileSize = DefaultTileSize;
        var maxDistanceSq = (int) (maxDistance * maxDistance);

        // if the explosion is centered on some grid (and not just space), get the transforms.
        if (referenceGrid != null)
        {
            var targetGrid = _mapManager.GetGrid(referenceGrid.Value);
            var xform = Transform(targetGrid.GridEntityId);
            targetAngle = xform.WorldRotation;
            targetMatrix = xform.InvWorldMatrix;
            tileSize = targetGrid.TileSize;
        }

        var offsetMatrix = Matrix3.Identity;
        offsetMatrix.R0C2 = tileSize / 2f;
        offsetMatrix.R1C2 = tileSize / 2f;

        // Here we can end up with a triple nested for loop:
        // foreach other grid
        //   foreach edge tile in that grid
        //     foreach tile in our grid that touches that tile (vast majority of the time: 1 tile, but could be up to 4)

        foreach (var gridToTransform in localGrids)
        {
            // we treat the target grid separately
            if (gridToTransform == referenceGrid)
                continue;

            if (!_gridEdges.TryGetValue(gridToTransform, out var edges))
                continue;

            if (!_mapManager.TryGetGrid(gridToTransform, out var grid))
                continue;

            if (grid.TileSize != tileSize)
            {
                Logger.Error($"Explosions do not support grids with different grid sizes. GridIds: {gridToTransform} and {referenceGrid}");
                continue;
            }

            var xforms = EntityManager.GetEntityQuery<TransformComponent>();
            var xform = xforms.GetComponent(grid.GridEntityId);
            var  (_, gridWorldRotation, gridWorldMatrix, invGridWorldMatrid) = xform.GetWorldPositionRotationMatrixWithInv(xforms);

            var localEpicentre = (Vector2i) invGridWorldMatrid.Transform(epicentre.Position);
            var matrix = offsetMatrix * gridWorldMatrix * targetMatrix;
            var angle = gridWorldRotation - targetAngle;

            var (x, y) = angle.RotateVec((tileSize / 4f, tileSize / 4f));

            foreach (var (tile, dir) in edges)
            {
                // if a tile is further than max distance from the epicentre, we just ignore it.
                var delta = tile - localEpicentre;
                if (delta.X * delta.X + delta.Y * delta.Y > maxDistanceSq) // no Vector2.Length???
                    continue;

                var center = matrix.Transform(tile);

                if ((dir & NeighborFlag.Cardinal) == 0)
                {
                    // this is purely a diagonal edge tile
                    var newIndex = new Vector2i((int) MathF.Floor(center.X), (int) MathF.Floor(center.Y));
                    if (!transformedEdges.TryGetValue(newIndex, out var data))
                    {
                        data = new();
                        transformedEdges[newIndex] = data;
                    }

                    data.BlockingGridEdges.Add(new(default, null, center, angle, tileSize));
                    continue;
                }

                // Instead of just mapping the center of the tile, we map for points on that tile. This is basically a
                // shitty approximation to doing a proper check to get all space-tiles that intersect this grid tile.
                // Not perfect, but works well enough.

                HashSet<Vector2i> transformedTiles = new()
                {
                    new((int) MathF.Floor(center.X + x), (int) MathF.Floor(center.Y + x)),  // center of tile, offset by (0.25, 0.25) in tile coordinates
                    new((int) MathF.Floor(center.X - y), (int) MathF.Floor(center.Y - y)),  // center offset by (-0.25, 0.25)
                    new((int) MathF.Floor(center.X - x), (int) MathF.Floor(center.Y + y)),  // offset by (-0.25, -0.25)
                    new((int) MathF.Floor(center.X + y), (int) MathF.Floor(center.Y - x)),  // offset by (0.25, -0.25)
                };

                foreach (var newIndices in transformedTiles)
                {
                    if (!transformedEdges.TryGetValue(newIndices, out var data))
                    {
                        data = new();
                        transformedEdges[newIndices] = data;
                    }
                    data.BlockingGridEdges.Add(new(tile, gridToTransform, center, angle, tileSize));
                }
            }
        }

        if (referenceGrid == null)
            return (transformedEdges, tileSize);

        // finally, we also include the blocking tiles from the reference grid.

        if (_gridEdges.TryGetValue(referenceGrid.Value, out var localEdges))
        {
            foreach (var (tile, dir) in localEdges)
            {
                // grids cannot overlap, so tile should never be an existing entry.
                // if this ever changes, this needs to do a try-get.
                var data = new BlockedSpaceTile();
                transformedEdges[tile] = data;

                data.UnblockedDirections = AtmosDirection.Invalid; // all directions are blocked automatically.

                if ((dir & NeighborFlag.Cardinal) == 0)
                    data.BlockingGridEdges.Add(new(default, null, ((Vector2) tile + 0.5f) * tileSize, 0, tileSize));
                else
                    data.BlockingGridEdges.Add(new(tile, referenceGrid.Value, ((Vector2) tile + 0.5f) * tileSize, 0, tileSize));
            }
        }

        return (transformedEdges, tileSize);
    }

    /// <summary>
    ///     Given an grid-edge blocking map, check if the blockers are allowed to propagate to each other through gaps in grids.
    /// </summary>
    /// <remarks>
    ///     After grid edges were transformed into the reference frame of some other grid, this function figures out
    ///     which of those edges are actually blocking explosion propagation.
    /// </remarks>
    public void GetUnblockedDirections(Dictionary<Vector2i, BlockedSpaceTile> transformedEdges, float tileSize)
    {
        foreach (var (tile, data) in transformedEdges)
        {
            if (data.UnblockedDirections == AtmosDirection.Invalid)
                continue; // already all blocked.

            var tileCenter = ((Vector2) tile + 0.5f) * tileSize;
            foreach (var edge in data.BlockingGridEdges)
            {
                // if a blocking edge contains the center of the tile, block all directions
                if (edge.Box.Contains(tileCenter))
                {
                    data.UnblockedDirections = AtmosDirection.Invalid;
                    break;
                }

                // check north
                if (edge.Box.Contains(tileCenter + (0, tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.North;

                // check south
                if (edge.Box.Contains(tileCenter + (0, -tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.South;

                // check east
                if (edge.Box.Contains(tileCenter + (tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.East;

                // check west
                if (edge.Box.Contains(tileCenter + (-tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.West;
            }
        }
    }

}
