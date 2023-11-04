using Content.Shared.NPC;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    public List<(Vector2i Start, Vector2i End)> MinimumSpanningTree(List<Vector2i> tiles, System.Random random)
    {
        // Generate connections between all rooms.
        var connections = new Dictionary<Vector2i, List<(Vector2i Tile, float Distance)>>(tiles.Count);

        foreach (var entrance in tiles)
        {
            var edgeConns = new List<(Vector2i Tile, float Distance)>(tiles.Count - 1);

            foreach (var other in tiles)
            {
                if (entrance == other)
                    continue;

                edgeConns.Add((other, (other - entrance).Length));
            }

            // Sort these as they will be iterated many times.
            edgeConns.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            connections.Add(entrance, edgeConns);
        }

        var seedIndex = random.Next(tiles.Count);
        var remaining = new ValueList<Vector2i>(tiles);
        remaining.RemoveAt(seedIndex);

        var edges = new List<(Vector2i Start, Vector2i End)>();

        var seedEntrance = tiles[seedIndex];
        var forest = new ValueList<Vector2i>(tiles.Count) { seedEntrance };

        while (remaining.Count > 0)
        {
            // Get cheapest edge
            var cheapestDistance = float.MaxValue;
            var cheapest = (Vector2i.Zero, Vector2i.Zero);

            foreach (var node in forest)
            {
                foreach (var conn in connections[node])
                {
                    // Existing tile, skip
                    if (forest.Contains(conn.Tile))
                        continue;

                    // Not the cheapest
                    if (cheapestDistance < conn.Distance)
                        continue;

                    cheapestDistance = conn.Distance;
                    cheapest = (node, conn.Tile);
                    // List is pre-sorted so we can just breakout easily.
                    break;
                }
            }

            DebugTools.Assert(cheapestDistance < float.MaxValue);
            // Add to tree
            edges.Add(cheapest);
            forest.Add(cheapest.Item2);
            remaining.Remove(cheapest.Item2);
        }

        return edges;
    }

    /// <summary>
    /// Primarily for dungeon usage.
    /// </summary>
    public void GetCorridorNodes(HashSet<Vector2i> corridorTiles,
        List<(Vector2i Start, Vector2i End)> edges,
        int pathLimit,
        HashSet<Vector2i>? forbiddenTiles = null,
        Func<Vector2i, float>? tileCallback = null)
    {
        // Pathfind each entrance
        var frontier = new PriorityQueue<Vector2i, float>();
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var directions = new Dictionary<Vector2i, Direction>();
        var costSoFar = new Dictionary<Vector2i, float>();
        forbiddenTiles ??= new HashSet<Vector2i>();

        foreach (var (start, end) in edges)
        {
            frontier.Clear();
            cameFrom.Clear();
            costSoFar.Clear();
            directions.Clear();
            directions[start] = Direction.Invalid;
            frontier.Enqueue(start, 0f);
            costSoFar[start] = 0f;
            var found = false;
            var count = 0;

            while (frontier.Count > 0 && count < pathLimit)
            {
                count++;
                var node = frontier.Dequeue();

                if (node == end)
                {
                    found = true;
                    break;
                }

                var lastDirection = directions[node];

                // Foreach neighbor etc etc
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        // Cardinals only.
                        if (x != 0 && y != 0)
                            continue;

                        var neighbor = new Vector2i(node.X + x, node.Y + y);

                        // FORBIDDEN
                        if (neighbor != end &&
                            forbiddenTiles.Contains(neighbor))
                        {
                            continue;
                        }

                        var tileCost = SharedPathfindingSystem.ManhattanDistance(node, neighbor);

                        // Weight towards existing corridors ig
                        if (corridorTiles.Contains(neighbor))
                        {
                            tileCost *= 0.10f;
                        }

                        var costMod = tileCallback?.Invoke(neighbor);
                        costMod ??= 1f;
                        tileCost *= costMod.Value;

                        var direction = (neighbor - node).GetCardinalDir();
                        directions[neighbor] = direction;

                        // If direction is different then penalise it.
                        if (direction != lastDirection)
                        {
                            tileCost *= 3f;
                        }

                        // f = g + h
                        // gScore is distance to the start node
                        // hScore is distance to the end node
                        var gScore = costSoFar[node] + tileCost;

                        if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                        {
                            continue;
                        }

                        cameFrom[neighbor] = node;
                        costSoFar[neighbor] = gScore;

                        // Make it greedy so multiply h-score to punish further nodes.
                        // This is necessary as we might have the deterredTiles multiplying towards the end
                        // so just finish it.
                        var hScore = SharedPathfindingSystem.ManhattanDistance(end, neighbor) * (1.0f - 1.0f / 1000.0f);
                        var fScore = gScore + hScore;
                        frontier.Enqueue(neighbor, fScore);
                    }
                }
            }

            // Rebuild path if it's valid.
            if (found)
            {
                var node = end;

                while (true)
                {
                    node = cameFrom[node];

                    // Don't want start or end nodes included.
                    if (node == start)
                        break;

                    corridorTiles.Add(node);
                }
            }
        }
    }
}
