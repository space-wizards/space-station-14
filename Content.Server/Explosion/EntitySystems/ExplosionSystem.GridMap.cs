using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Explosion.EntitySystems;

// This partial part of the explosion system has all of the functions used to facilitate explosions moving across grids.
// A good portion of it is focused around keeping track of what tile-indices on a grid correspond to tiles that border
// space. AFAIK no other system currently needs to track these "edge-tiles". If they do, this should probably be a
// property of the grid itself?
public sealed partial class ExplosionSystem
{
    /// <summary>
    ///     Set of tiles of each grid that are directly adjacent to space, along with the directions that face space.
    /// </summary>
    private Dictionary<EntityUid, Dictionary<Vector2i, NeighborFlag>> _gridEdges = new();

    /// <summary>
    ///     On grid startup, prepare a map of grid edges.
    /// </summary>
    private void OnGridStartup(GridStartupEvent ev)
    {
        var grid = Comp<MapGridComponent>(ev.EntityUid);

        Dictionary<Vector2i, NeighborFlag> edges = new();
        _gridEdges[ev.EntityUid] = edges;

        foreach (var tileRef in _map.GetAllTiles(ev.EntityUid, grid))
        {
            if (IsEdge(grid, tileRef.GridIndices, out var dir))
                edges.Add(tileRef.GridIndices, dir);
        }
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _airtightMap.Remove(ev.EntityUid);
        _gridEdges.Remove(ev.EntityUid);

        // this should be a small enough set that iterating all of them is fine
        var query = EntityQueryEnumerator<ExplosionVisualsComponent>();
        while (query.MoveNext(out var visuals))
        {
            visuals.Tiles.Remove(ev.EntityUid);
        }
    }

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

        var targetMatrix = Matrix3x2.Identity;
        Angle targetAngle = new();
        var tileSize = DefaultTileSize;
        var maxDistanceSq = (int) (maxDistance * maxDistance);

        // if the explosion is centered on some grid (and not just space), get the transforms.
        if (referenceGrid != null)
        {
            var targetGrid = Comp<MapGridComponent>(referenceGrid.Value);
            var xform = Transform(referenceGrid.Value);
            (_, targetAngle, targetMatrix) = _transformSystem.GetWorldPositionRotationInvMatrix(xform);
            tileSize = targetGrid.TileSize;
        }

        var offsetMatrix = Matrix3x2.Identity;
        offsetMatrix.M31 = tileSize / 2f;
        offsetMatrix.M32 = tileSize / 2f;

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

            if (!TryComp(gridToTransform, out MapGridComponent? grid))
                continue;

            if (grid.TileSize != tileSize)
            {
                Log.Error($"Explosions do not support grids with different grid sizes. GridIds: {gridToTransform} and {referenceGrid}");
                continue;
            }

            var xforms = EntityManager.GetEntityQuery<TransformComponent>();
            var xform = xforms.GetComponent(gridToTransform);
            var  (_, gridWorldRotation, gridWorldMatrix, invGridWorldMatrid) = _transformSystem.GetWorldPositionRotationMatrixWithInv(xform, xforms);

            var localEpicentre = (Vector2i) Vector2.Transform(epicentre.Position, invGridWorldMatrid);
            var matrix = offsetMatrix * gridWorldMatrix * targetMatrix;
            var angle = gridWorldRotation - targetAngle;

            var (x, y) = angle.RotateVec(new Vector2(tileSize / 4f, tileSize / 4f));

            foreach (var (tile, dir) in edges)
            {
                // if a tile is further than max distance from the epicentre, we just ignore it.
                var delta = tile - localEpicentre;
                if (delta.X * delta.X + delta.Y * delta.Y > maxDistanceSq) // no Vector2.Length???
                    continue;

                var center = Vector2.Transform(tile, matrix);

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
                    data.BlockingGridEdges.Add(new(default, null, (tile + Vector2Helpers.Half) * tileSize, 0, tileSize));
                else
                    data.BlockingGridEdges.Add(new(tile, referenceGrid.Value, (tile + Vector2Helpers.Half) * tileSize, 0, tileSize));
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

            var tileCenter = (tile + new Vector2(0.5f, 0.5f)) * tileSize;
            foreach (var edge in data.BlockingGridEdges)
            {
                // if a blocking edge contains the center of the tile, block all directions
                if (edge.Box.Contains(tileCenter))
                {
                    data.UnblockedDirections = AtmosDirection.Invalid;
                    break;
                }

                // check north
                if (edge.Box.Contains(tileCenter + new Vector2(0, tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.North;

                // check south
                if (edge.Box.Contains(tileCenter + new Vector2(0, -tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.South;

                // check east
                if (edge.Box.Contains(tileCenter + new Vector2(tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.East;

                // check west
                if (edge.Box.Contains(tileCenter + new Vector2(-tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.West;
            }
        }
    }

    /// <summary>
    ///     When a tile is updated, we might need to update the grid edge maps.
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent ev)
    {
        // only need to update the grid-edge map if a tile was added or removed from the grid.
        if (!ev.NewTile.Tile.IsEmpty && !ev.OldTile.IsEmpty)
            return;

        if (!TryComp(ev.Entity, out MapGridComponent? grid))
            return;

        var tileRef = ev.NewTile;

        if (!_gridEdges.TryGetValue(tileRef.GridUid, out var edges))
        {
            edges = new();
            _gridEdges[tileRef.GridUid] = edges;
        }

        if (tileRef.Tile.IsEmpty)
        {
            // if the tile is empty, it cannot itself be an edge tile.
            edges.Remove(tileRef.GridIndices);

            // add any valid neighbours to the list of edge-tiles
            for (var i = 0; i < NeighbourVectors.Length; i++)
            {
                var neighbourIndex = tileRef.GridIndices + NeighbourVectors[i];

                if (grid.TryGetTileRef(neighbourIndex, out var neighbourTile) && !neighbourTile.Tile.IsEmpty)
                {
                    var oppositeDirection = (NeighborFlag) (1 << ((i + 4) % 8));
                    edges[neighbourIndex] = edges.GetValueOrDefault(neighbourIndex) | oppositeDirection;
                }
            }

            return;
        }

        // the tile is not empty space, but was previously. So update directly adjacent neighbours, which may no longer
        // be edge tiles.
        for (var i = 0; i < NeighbourVectors.Length; i++)
        {
            var neighbourIndex = tileRef.GridIndices + NeighbourVectors[i];

            if (edges.TryGetValue(neighbourIndex, out var neighborSpaceDir))
            {
                var oppositeDirection = (NeighborFlag) (1 << ((i + 4) % 8));
                neighborSpaceDir &= ~oppositeDirection;
                if (neighborSpaceDir == NeighborFlag.Invalid)
                {
                    // no longer an edge tile
                    edges.Remove(neighbourIndex);
                    continue;
                }

                edges[neighbourIndex] = neighborSpaceDir;
            }
        }

        // finally check if the new tile is itself an edge tile
        if (IsEdge(grid, tileRef.GridIndices, out var spaceDir))
            edges.Add(tileRef.GridIndices, spaceDir);
    }

    /// <summary>
    ///     Check whether a tile is on the edge of a grid (i.e., whether it borders space).
    /// </summary>
    /// <remarks>
    ///     Optionally ignore a specific Vector2i. Used by <see cref="OnTileChanged"/> when we already know that a
    ///     given tile is not space. This avoids unnecessary TryGetTileRef calls.
    /// </remarks>
    private bool IsEdge(MapGridComponent grid, Vector2i index, out NeighborFlag spaceDirections)
    {
        spaceDirections = NeighborFlag.Invalid;
        for (var i = 0; i < NeighbourVectors.Length; i++)
        {
            if (!grid.TryGetTileRef(index + NeighbourVectors[i], out var neighborTile) || neighborTile.Tile.IsEmpty)
                spaceDirections |= (NeighborFlag) (1 << i);
        }

        return spaceDirections != NeighborFlag.Invalid;
    }

    // yeah this is now the third direction flag enum, and the 5th (afaik) direction enum overall.....
    /// <summary>
    ///     Directional bitflags used to denote the neighbouring tiles of some tile on a grid.. Differ from atmos and
    ///     normal directional flags as NorthEast != North | East
    /// </summary>
    [Flags]
    public enum NeighborFlag : byte
    {
        Invalid = 0,
        North = 1 << 0,
        NorthEast = 1 << 1,
        East = 1 << 2,
        SouthEast = 1 << 3,
        South = 1 << 4,
        SouthWest = 1 << 5,
        West = 1 << 6,
        NorthWest = 1 << 7,

        Cardinal = North | East | South | West,
        Diagonal = NorthEast | SouthEast | SouthWest | NorthWest,
        Any = Cardinal | Diagonal
    }

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
}

/// <summary>
///     This class has information about the space equivalent of an airtight entity blocking explosions: the edges of grids.
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
    public List<GridEdgeData> BlockingGridEdges = new();

    public sealed class GridEdgeData
    {
        public Vector2i Tile;
        public EntityUid? Grid;
        public Box2Rotated Box;

        public GridEdgeData(Vector2i tile, EntityUid? grid, Vector2 center, Angle angle, float size)
        {
            Tile = tile;
            Grid = grid;
            Box = new(Box2.CenteredAround(center, new Vector2(size, size)), angle, center);
        }
    }
}
