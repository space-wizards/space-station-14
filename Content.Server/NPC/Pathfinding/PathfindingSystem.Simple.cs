namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Gets simple A* path from start to end. Can also supply an optional tile-cost for tiles.
    /// </summary>
    public SimplePathResult GetPath(PathArgs args, Func<Vector2i, float>? tileCostFunc = null)
    {
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var costSoFar = new Dictionary<Vector2i, float>();
        var frontier = new PriorityQueue<Vector2i, float>();

        frontier.Enqueue(args.Start, 0f);
        var count = 0;

        while (frontier.TryDequeue(out var node, out _) && count < args.Limit)
        {
            count++;

            if (node == args.End)
            {
                // Found target
                var path = ReconstructPath(args.End, cameFrom);

                return new SimplePathResult()
                {
                    CameFrom = cameFrom,
                    Path = path,
                };
            }

            if (args.Diagonals)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var neighbor = node + new Vector2i(x, y);
                        var neighborCost = OctileDistance(node, neighbor) * tileCostFunc?.Invoke(neighbor) ?? 1f;

                        if (neighborCost.Equals(0f))
                        {
                            continue;
                        }

                        // f = g + h
                        // gScore is distance to the start node
                        // hScore is distance to the end node
                        var gScore = costSoFar[node] + neighborCost;

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
                        var hScore = OctileDistance(args.End, neighbor) * (1.0f + 1.0f / 1000.0f);
                        var fScore = gScore + hScore;
                        frontier.Enqueue(neighbor, fScore);
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
                        var neighborCost = ManhattanDistance(node, neighbor) * tileCostFunc?.Invoke(neighbor) ?? 1f;

                        if (neighborCost.Equals(0f))
                            continue;

                        var gScore = costSoFar[node] + neighborCost;

                        if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                            continue;

                        cameFrom[neighbor] = node;
                        costSoFar[neighbor] = gScore;

                        var hScore = ManhattanDistance(args.End, neighbor) * (1.0f + 1.0f / 1000.0f);
                        var fScore = gScore + hScore;
                        frontier.Enqueue(neighbor, fScore);
                    }
                }
            }
        }

        return SimplePathResult.NoPath;
    }

    private List<Vector2i> ReconstructPath(Vector2i end, Dictionary<Vector2i, Vector2i> cameFrom)
    {
        var path = new List<Vector2i>()
        {
            end,
        };
        var node = end;

        while (cameFrom.TryGetValue(node, out var source))
        {
            path.Add(source);
            node = source;
        }

        return path;
    }

    /// <summary>
    /// Pathfinding args for a 1-1 path.
    /// </summary>
    public record struct PathArgs()
    {
        public Vector2i Start;
        public Vector2i End;

        public bool Diagonals = false;

        public int Limit = 10000;
    }

    public record struct SimplePathResult
    {
        public static SimplePathResult NoPath = new();

        public List<Vector2i> Path;
        public Dictionary<Vector2i, Vector2i> CameFrom;
    }
}
