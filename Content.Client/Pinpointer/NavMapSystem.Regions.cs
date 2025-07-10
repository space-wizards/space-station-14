using Content.Shared.Atmos;
using Content.Shared.Pinpointer;
using System.Linq;

namespace Content.Client.Pinpointer;

public sealed partial class NavMapSystem
{
    private (AtmosDirection, Vector2i, AtmosDirection)[] _regionPropagationTable =
    {
        (AtmosDirection.East, new Vector2i(1, 0), AtmosDirection.West),
        (AtmosDirection.West, new Vector2i(-1, 0), AtmosDirection.East),
        (AtmosDirection.North, new Vector2i(0, 1), AtmosDirection.South),
        (AtmosDirection.South, new Vector2i(0, -1), AtmosDirection.North),
    };

    public override void Update(float frameTime)
    {
        // To prevent compute spikes, only one region is flood filled per frame 
        var query = AllEntityQuery<NavMapComponent>();

        while (query.MoveNext(out var ent, out var entNavMapRegions))
            FloodFillNextEnqueuedRegion(ent, entNavMapRegions);
    }

    private void FloodFillNextEnqueuedRegion(EntityUid uid, NavMapComponent component)
    {
        if (!component.QueuedRegionsToFlood.Any())
            return;

        var regionOwner = component.QueuedRegionsToFlood.Dequeue();

        // If the region is no longer valid, flood the next one in the queue
        if (!component.RegionProperties.TryGetValue(regionOwner, out var regionProperties) ||
            !regionProperties.Seeds.Any())
        {
            FloodFillNextEnqueuedRegion(uid, component);
            return;
        }

        // Flood fill the region, using the region seeds as starting points
        var (floodedTiles, floodedChunks) = FloodFillRegion(uid, component, regionProperties);

        // Combine the flooded tiles into larger rectangles
        var gridCoords = GetMergedRegionTiles(floodedTiles);

        // Create and assign the new region overlay
        var regionOverlay = new NavMapRegionOverlay(regionProperties.UiKey, gridCoords)
        {
            Color = regionProperties.Color
        };

        component.RegionOverlays[regionOwner] = regionOverlay;

        // To reduce unnecessary future flood fills, we will track which chunks have been flooded by a region owner

        // First remove an old assignments
        if (component.RegionOwnerToChunkTable.TryGetValue(regionOwner, out var oldChunks))
        {
            foreach (var chunk in oldChunks)
            {
                if (component.ChunkToRegionOwnerTable.TryGetValue(chunk, out var oldOwners))
                {
                    oldOwners.Remove(regionOwner);
                    component.ChunkToRegionOwnerTable[chunk] = oldOwners;
                }
            }
        }

        // Now update with the new assignments
        component.RegionOwnerToChunkTable[regionOwner] = floodedChunks;

        foreach (var chunk in floodedChunks)
        {
            if (!component.ChunkToRegionOwnerTable.TryGetValue(chunk, out var owners))
                owners = new();

            owners.Add(regionOwner);
            component.ChunkToRegionOwnerTable[chunk] = owners;
        }
    }

    private (HashSet<Vector2i>, HashSet<Vector2i>) FloodFillRegion(EntityUid uid, NavMapComponent component, NavMapRegionProperties regionProperties)
    {
        if (!regionProperties.Seeds.Any())
            return (new(), new());

        var visitedChunks = new HashSet<Vector2i>();
        var visitedTiles = new HashSet<Vector2i>();
        var tilesToVisit = new Stack<Vector2i>();

        foreach (var regionSeed in regionProperties.Seeds)
        {
            tilesToVisit.Push(regionSeed);

            while (tilesToVisit.Count > 0)
            {
                // If the max region area is hit, exit
                if (visitedTiles.Count > regionProperties.MaxArea)
                    return (new(), new());

                // Pop the top tile from the stack 
                var current = tilesToVisit.Pop();

                // If the current tile position has already been visited,
                // or is too far away from the seed, continue
                if ((regionSeed - current).Length > regionProperties.MaxRadius)
                    continue;

                if (visitedTiles.Contains(current))
                    continue;

                // Determine the tile's chunk index
                var chunkOrigin = SharedMapSystem.GetChunkIndices(current, ChunkSize);
                var relative = SharedMapSystem.GetChunkRelative(current, ChunkSize);
                var idx = GetTileIndex(relative);

                // Extract the tile data
                if (!component.Chunks.TryGetValue(chunkOrigin, out var chunk))
                    continue;

                var flag = chunk.TileData[idx];

                // If the current tile is entirely occupied, continue
                if ((FloorMask & flag) == 0)
                    continue;

                if ((WallMask & flag) == WallMask)
                    continue;

                if ((AirlockMask & flag) == AirlockMask)
                    continue;

                // Otherwise the tile can be added to this region
                visitedTiles.Add(current);
                visitedChunks.Add(chunkOrigin);

                // Determine if we can propagate the region into its cardinally adjacent neighbors
                // To propagate to a neighbor, movement into the neighbors closest edge must not be 
                // blocked, and vice versa

                foreach (var (direction, tileOffset, reverseDirection) in _regionPropagationTable)
                {
                    if (!RegionCanPropagateInDirection(chunk, current, direction))
                        continue;

                    var neighbor = current + tileOffset;
                    var neighborOrigin = SharedMapSystem.GetChunkIndices(neighbor, ChunkSize);

                    if (!component.Chunks.TryGetValue(neighborOrigin, out var neighborChunk))
                        continue;

                    visitedChunks.Add(neighborOrigin);

                    if (!RegionCanPropagateInDirection(neighborChunk, neighbor, reverseDirection))
                        continue;

                    tilesToVisit.Push(neighbor);
                }
            }
        }

        return (visitedTiles, visitedChunks);
    }

    private bool RegionCanPropagateInDirection(NavMapChunk chunk, Vector2i tile, AtmosDirection direction)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var idx = GetTileIndex(relative);
        var flag = chunk.TileData[idx];

        if ((FloorMask & flag) == 0)
            return false;

        var directionMask = 1 << (int)direction;
        var wallMask = (int)direction << (int)NavMapChunkType.Wall;
        var airlockMask = (int)direction << (int)NavMapChunkType.Airlock;

        if ((wallMask & flag) > 0)
            return false;

        if ((airlockMask & flag) > 0)
            return false;

        return true;
    }

    private List<(Vector2i, Vector2i)> GetMergedRegionTiles(HashSet<Vector2i> tiles)
    {
        if (!tiles.Any())
            return new();

        var x = tiles.Select(t => t.X);
        var minX = x.Min();
        var maxX = x.Max();

        var y = tiles.Select(t => t.Y);
        var minY = y.Min();
        var maxY = y.Max();

        var matrix = new int[maxX - minX + 1, maxY - minY + 1];

        foreach (var tile in tiles)
        {
            var a = tile.X - minX;
            var b = tile.Y - minY;

            matrix[a, b] = 1;
        }

        return GetMergedRegionTiles(matrix, new Vector2i(minX, minY));
    }

    private List<(Vector2i, Vector2i)> GetMergedRegionTiles(int[,] matrix, Vector2i offset)
    {
        var output = new List<(Vector2i, Vector2i)>();

        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);

        var dp = new int[rows, cols];
        var coords = (new Vector2i(), new Vector2i());
        var maxArea = 0;

        var count = 0;

        while (!IsArrayEmpty(matrix))
        {
            count++;

            if (count > rows * cols)
                break;

            // Clear old values
            dp = new int[rows, cols];
            coords = (new Vector2i(), new Vector2i());
            maxArea = 0;

            // Initialize the first row of dp
            for (int j = 0; j < cols; j++)
            {
                dp[0, j] = matrix[0, j];
            }

            // Calculate dp values for remaining rows
            for (int i = 1; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    dp[i, j] = matrix[i, j] == 1 ? dp[i - 1, j] + 1 : 0;
            }

            // Find the largest rectangular area seeded for each position in the matrix
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int minWidth = dp[i, j];

                    for (int k = j; k >= 0; k--)
                    {
                        if (dp[i, k] <= 0)
                            break;

                        minWidth = Math.Min(minWidth, dp[i, k]);
                        var currArea = Math.Max(maxArea, minWidth * (j - k + 1));

                        if (currArea > maxArea)
                        {
                            maxArea = currArea;
                            coords = (new Vector2i(i - minWidth + 1, k), new Vector2i(i, j));
                        }
                    }
                }
            }

            // Save the recorded rectangle vertices
            output.Add((coords.Item1 + offset, coords.Item2 + offset));

            // Removed the tiles covered by the rectangle from matrix
            for (int i = coords.Item1.X; i <= coords.Item2.X; i++)
            {
                for (int j = coords.Item1.Y; j <= coords.Item2.Y; j++)
                    matrix[i, j] = 0;
            }
        }

        return output;
    }

    private bool IsArrayEmpty(int[,] matrix)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                if (matrix[i, j] == 1)
                    return false;
            }
        }

        return true;
    }
}
