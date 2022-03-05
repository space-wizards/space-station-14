using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Explosion.EntitySystems;

// This partial part of the explosion system has all of the functions used to facilitate explosions moving across grids.
// A good portion of it is focused around keeping track of what tile-indices on a grid correspond to tiles that border
// space. AFAIK no other system currently needs to track these "edge-tiles". If they do, this should probably be a
// property of the grid itself?
public sealed partial class ExplosionSystem : EntitySystem
{
    /// <summary>
    ///     Set of tiles of each grid that are directly adjacent to space, along with the directions that face space.
    /// </summary>
    private Dictionary<GridId, Dictionary<Vector2i, AtmosDirection>> _gridEdges = new();

    /// <summary>
    ///     Set of tiles of each grid that are diagonally adjacent to space
    /// </summary>
    private Dictionary<GridId, HashSet<Vector2i>> _diagGridEdges = new();

    /// <summary>
    ///     On grid startup, prepare a map of grid edges.
    /// </summary>
    private void OnGridStartup(GridStartupEvent ev)
    {
        if (!_mapManager.TryGetGrid(ev.GridId, out var grid))
            return;

        Dictionary<Vector2i, AtmosDirection> edges = new();
        HashSet<Vector2i> diagEdges = new();
        _gridEdges[ev.GridId] = edges;
        _diagGridEdges[ev.GridId] = diagEdges;

        foreach (var tileRef in grid.GetAllTiles())
        {
            if (tileRef.Tile.IsEmpty)
                continue;

            if (IsEdge(grid, tileRef.GridIndices, out var dir))
                edges.Add(tileRef.GridIndices, dir);
            else if (IsDiagonalEdge(grid, tileRef.GridIndices))
                diagEdges.Add(tileRef.GridIndices);
        }
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _airtightMap.Remove(ev.GridId);
        _gridEdges.Remove(ev.GridId);
        _diagGridEdges.Remove(ev.GridId);
    }

    /// <summary>
    ///     Take our map of grid edges, where each is defined in their own grid's reference frame, and map those
    ///     edges all onto one grids reference frame.
    /// </summary>
    public (Dictionary<Vector2i, GridBlockData>, ushort) TransformGridEdges(MapId targetMap, GridId? referenceGrid, List<GridId> localGrids)
    {
        Dictionary<Vector2i, GridBlockData> transformedEdges = new();

        var targetMatrix = Matrix3.Identity;
        Angle targetAngle = new();
        ushort tileSize = DefaultTileSize;

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
        offsetMatrix.R0C2 = tileSize / 2;
        offsetMatrix.R1C2 = tileSize / 2;

        // here we will get a triple nested for loop:
        // foreach other grid
        //   foreach edge tile in that grid
        //     foreach tile in our grid that touches that tile (vast majority of the time: 1 tile, but could be up to 4)

        HashSet<Vector2i> transformedTiles = new();
        foreach (var gridToTransform in localGrids)
        {
            // we treat the target grid separately
            if (gridToTransform == referenceGrid)
                continue;

            if (!_gridEdges.TryGetValue(gridToTransform, out var edges))
                continue;

            if (!_mapManager.TryGetGrid(gridToTransform, out var grid) ||
                grid.ParentMapId != targetMap)
                continue;

            if (grid.TileSize != tileSize)
            {
                Logger.Error($"Explosions do not support grids with different grid sizes. GridIds: {gridToTransform} and {referenceGrid}");
                continue;
            }

            var xform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);
            var matrix = offsetMatrix * xform.WorldMatrix * targetMatrix;
            var angle = xform.WorldRotation - targetAngle;

            var (x, y) = angle.RotateVec((tileSize / 4, tileSize / 4));

            foreach (var (tile, dir) in edges)
            {
                var center = matrix.Transform(tile);

                // this tile might touch several other tiles, or maybe just one tile. Here we use a Vector2i HashSet to
                // remove duplicates.
                transformedTiles.Clear();
                transformedTiles.Add(new((int) MathF.Floor(center.X + x), (int) MathF.Floor(center.Y + y)));  // initial direction
                transformedTiles.Add(new((int) MathF.Floor(center.X - y), (int) MathF.Floor(center.Y + x)));  // rotated 90 degrees
                transformedTiles.Add(new((int) MathF.Floor(center.X - x), (int) MathF.Floor(center.Y - y)));  // rotated 180 degrees
                transformedTiles.Add(new((int) MathF.Floor(center.X + y), (int) MathF.Floor(center.Y - x)));  // rotated 270 degrees

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

        // Next we transform any diagonal edges.
        Vector2i newIndex;
        foreach (var gridToTransform in localGrids)
        {
            // we treat the target grid separately
            if (gridToTransform == referenceGrid)
                continue;

            if (!_diagGridEdges.TryGetValue(gridToTransform, out var diagEdges))
                continue;

            if (!_mapManager.TryGetGrid(gridToTransform, out var grid) ||
                grid.ParentMapId != targetMap)
                continue;

            if (grid.TileSize != tileSize)
            {
                Logger.Error($"Explosions do not support grids with different grid sizes. GridIds: {gridToTransform} and {referenceGrid}");
                continue;
            }

            var xform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);
            var matrix = offsetMatrix * xform.WorldMatrix * targetMatrix;
            var angle = xform.WorldRotation - targetAngle;

            foreach (var tile in diagEdges)
            {
                var center = matrix.Transform(tile);
                newIndex = new((int) MathF.Floor(center.X), (int) MathF.Floor(center.Y));
                if (!transformedEdges.TryGetValue(newIndex, out var data))
                {
                    data = new();
                    transformedEdges[newIndex] = data;
                }

                // explosions are not allowed to propagate diagonally ONTO grids. so we just use defaults for some fields.
                data.BlockingGridEdges.Add(new(default, null, center, angle, tileSize));
            }
        }

        if (referenceGrid == null)
            return (transformedEdges, tileSize);

        // finally, we also include the blocking tiles from the reference grid.

        if (_gridEdges.TryGetValue(referenceGrid.Value, out var localEdges))
        {
            foreach (var (tile, _) in localEdges)
            {
                // grids cannot overlap, so tile should NEVER be an existing entry.
                var data = new GridBlockData();
                transformedEdges[tile] = data;
                
                data.UnblockedDirections = AtmosDirection.Invalid; // all directions are blocked automatically.
                data.BlockingGridEdges.Add(new(tile, referenceGrid.Value, ((Vector2) tile + 0.5f) * tileSize, 0, tileSize));
            }
        }

        if (_diagGridEdges.TryGetValue(referenceGrid.Value, out var localDiagEdges))
        {
            foreach (var tile in localDiagEdges)
            {

                // grids cannot overlap, so tile should NEVER be an existing entry.
                var data = new GridBlockData();
                transformedEdges[tile] = data;

                data.UnblockedDirections = AtmosDirection.Invalid; // all directions are blocked automatically.
                data.BlockingGridEdges.Add(new(default, null, ((Vector2) tile + 0.5f) * tileSize, 0, tileSize));
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
    public void GetUnblockedDirections(Dictionary<Vector2i, GridBlockData> transformedEdges, ushort tileSize)
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
                if (edge.Box.Contains(tileCenter + (0, tileSize / 2)))
                    data.UnblockedDirections &= ~AtmosDirection.North;

                // check south
                if (edge.Box.Contains(tileCenter + (0, -tileSize / 2)))
                    data.UnblockedDirections &= ~AtmosDirection.South;

                // check east
                if (edge.Box.Contains(tileCenter + (tileSize / 2, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.East;

                // check west
                if (edge.Box.Contains(tileCenter + (-tileSize / 2, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.West;
            }
        }
    }

    /// <summary>
    ///     When a tile is updated, we might need to update the grid edge maps.
    /// </summary>
    private void OnTileChanged(object? sender, TileChangedEventArgs e)
    {
        // only need to update the grid-edge map if the tile changed from space to not-space.
        if (!e.NewTile.Tile.IsEmpty && !e.OldTile.IsEmpty)
            return;

        var tileRef = e.NewTile;

        if (!_mapManager.TryGetGrid(tileRef.GridIndex, out var grid))
            return;

        if (!_gridEdges.TryGetValue(tileRef.GridIndex, out var edges))
        {
            edges = new();
            _gridEdges[tileRef.GridIndex] = edges;
        }

        if (!_diagGridEdges.TryGetValue(tileRef.GridIndex, out var diagEdges))
        {
            diagEdges = new();
            _diagGridEdges[tileRef.GridIndex] = diagEdges;
        }

        if (tileRef.Tile.IsEmpty)
        {
            // if the tile is empty, it cannot itself be an edge tile.
            edges.Remove(tileRef.GridIndices);
            diagEdges.Remove(tileRef.GridIndices);

            // add any valid neighbours to the list of edge-tiles
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);

                var neighbourIndex = tileRef.GridIndices.Offset(direction);

                if (grid.TryGetTileRef(neighbourIndex, out var neighbourTile) && !neighbourTile.Tile.IsEmpty)
                {
                    edges[neighbourIndex] = edges.GetValueOrDefault(neighbourIndex) | direction.GetOpposite();
                    diagEdges.Remove(neighbourIndex);
                }
            }

            foreach (var diagNeighbourIndex in GetDiagonalNeighbors(tileRef.GridIndices))
            {
                if (edges.ContainsKey(diagNeighbourIndex))
                    continue;

                if (grid.TryGetTileRef(diagNeighbourIndex, out var neighbourIndex) && !neighbourIndex.Tile.IsEmpty)
                    diagEdges.Add(diagNeighbourIndex);
            }

            return;
        }

        // the tile is not empty space, but was previously. So update directly adjacent neighbours, which may no longer
        // be edge tiles.
        AtmosDirection spaceDir;
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            var neighbourIndex = tileRef.GridIndices.Offset(direction);

            if (edges.TryGetValue(neighbourIndex, out spaceDir))
            {
                spaceDir = spaceDir & ~direction.GetOpposite();
                if (spaceDir != AtmosDirection.Invalid)
                    edges[neighbourIndex] = spaceDir;
                else
                {
                    // no longer a direct edge ...
                    edges.Remove(neighbourIndex);

                    // ... but it could now be a diagonal edge
                    if (IsDiagonalEdge(grid, neighbourIndex, tileRef.GridIndices))
                        diagEdges.Add(neighbourIndex);
                }
            }
        }

        // and again for diagonal neighbours
        foreach (var neighborIndex in GetDiagonalNeighbors(tileRef.GridIndices))
        {
            if (diagEdges.Contains(neighborIndex) && !IsDiagonalEdge(grid, neighborIndex, tileRef.GridIndices))
                diagEdges.Remove(neighborIndex);
        }

        // finally check if the new tile is itself an edge tile
        if (IsEdge(grid, tileRef.GridIndices, out spaceDir))
            edges.Add(tileRef.GridIndices, spaceDir);
        else if (IsDiagonalEdge(grid, tileRef.GridIndices))
            diagEdges.Add(tileRef.GridIndices);
    }

    /// <summary>
    ///     Check whether a tile is on the edge of a grid (i.e., whether it borders space).
    /// </summary>
    /// <remarks>
    ///     Optionally ignore a specific Vector2i. Used by <see cref="OnTileChanged"/> when we already know that a
    ///     given tile is not space. This avoids unnecessary TryGetTileRef calls.
    /// </remarks>
    private bool IsEdge(IMapGrid grid, Vector2i index, out AtmosDirection spaceDirections)
    {
        spaceDirections = AtmosDirection.Invalid;
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);

            if (!grid.TryGetTileRef(index.Offset(direction), out var neighborTile) || neighborTile.Tile.IsEmpty)
                spaceDirections |= direction;
        }

        return spaceDirections != AtmosDirection.Invalid;
    }

    private bool IsDiagonalEdge(IMapGrid grid, Vector2i index, Vector2i? ignore = null)
    {
        foreach (var neighbourIndex in GetDiagonalNeighbors(index))
        {
            if (neighbourIndex == ignore)
                continue;

            if (!grid.TryGetTileRef(neighbourIndex, out var neighborTile) || neighborTile.Tile.IsEmpty)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Enumerate over diagonally adjacent tiles.
    /// </summary>
    internal static IEnumerable<Vector2i> GetDiagonalNeighbors(Vector2i pos)
    {
        yield return pos + (1, 1);
        yield return pos + (-1, -1);
        yield return pos + (1, -1);
        yield return pos + (-1, 1);
    }
}

public struct GridEdgeData : IEquatable<GridEdgeData>
{
    public Vector2i Tile;
    public GridId? Grid;
    public Box2Rotated Box;

    public GridEdgeData(Vector2i tile, GridId? grid, Vector2 center, Angle angle, float size)
    {
        Tile = tile;
        Grid = grid;
        Box = new(Box2.CenteredAround(center, (size, size)), angle, center);
    }

    /// <inheritdoc />
    public bool Equals(GridEdgeData other)
    {
        return Tile.Equals(other.Tile) && Grid.Equals(other.Grid);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (Tile.GetHashCode() * 397) ^ Grid.GetHashCode();
        }
    }
}

public record GridBlockData
{
    /// <summary>
    ///     What directions of this tile are not blocked by some other grid?
    /// </summary>
    public AtmosDirection UnblockedDirections = AtmosDirection.All;

    /// <summary>
    ///     Hashset contains information about the edge-tiles, which belong to some other grid(s), that are blocking
    ///     this tile.
    /// </summary>
    public HashSet<GridEdgeData> BlockingGridEdges = new();
}
