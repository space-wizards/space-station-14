namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /*
     * Handle BFS searches from Start->End. Doesn't consider NPC pathfinding.
     */

    /// <summary>
    /// Pathfinding args for a 1-many path.
    /// </summary>
    public record struct BreadthPathArgs()
    {
        public Vector2i Start;
        public List<Vector2i> Ends;

        public bool Diagonals = false;

        public Func<Vector2i, float>? TileCost;

        public int Limit = 10000;
    }

    /// <summary>
    /// Gets a BFS path from start to any end. Can also supply an optional tile-cost for tiles.
    /// </summary>
    public SimplePathResult GetBreadthPath(BreadthPathArgs args)
    {
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var costSoFar = new Dictionary<Vector2i, float>();
        var frontier = new PriorityQueue<Vector2i, float>();

        costSoFar[args.Start] = 0f;
        frontier.Enqueue(args.Start, 0f);
        var count = 0;

        while (frontier.TryDequeue(out var node, out _) && count < args.Limit)
        {
            count++;

            if (args.Ends.Contains(node))
            {
                // Found target
                var path = ReconstructPath(node, cameFrom);

                return new SimplePathResult()
                {
                    CameFrom = cameFrom,
                    Path = path,
                };
            }

            var gCost = costSoFar[node];

            if (args.Diagonals)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var neighbor = node + new Vector2i(x, y);
                        var neighborCost = OctileDistance(node, neighbor) * args.TileCost?.Invoke(neighbor) ?? 1f;

                        if (neighborCost.Equals(0f))
                        {
                            continue;
                        }

                        // f = g + h
                        // gScore is distance to the start node
                        // hScore is distance to the end node
                        var gScore = gCost + neighborCost;

                        // Slower to get here so just ignore it.
                        if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                        {
                            continue;
                        }

                        cameFrom[neighbor] = node;
                        costSoFar[neighbor] = gScore;
                        // pFactor is tie-breaker where the fscore is otherwise equal.
                        // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                        // There's other ways to do it but future consideration
                        // The closer the fScore is to the actual distance then the better the pathfinder will be
                        // (i.e. somewhere between 1 and infinite)
                        // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                        frontier.Enqueue(neighbor, gScore);
                    }
                }
            }
            else
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x != 0 && y != 0)
                            continue;

                        var neighbor = node + new Vector2i(x, y);
                        var neighborCost = ManhattanDistance(node, neighbor) * args.TileCost?.Invoke(neighbor) ?? 1f;

                        if (neighborCost.Equals(0f))
                            continue;

                        var gScore = gCost + neighborCost;

                        if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                            continue;

                        cameFrom[neighbor] = node;
                        costSoFar[neighbor] = gScore;

                        frontier.Enqueue(neighbor, gScore);
                    }
                }
            }
        }

        return SimplePathResult.NoPath;
    }
}
