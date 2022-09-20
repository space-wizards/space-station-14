using System.Threading.Tasks;
using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    private sealed class PathComparer : IComparer<ValueTuple<float, PathPoly>>
    {
        public int Compare((float, PathPoly) x, (float, PathPoly) y)
        {
            return y.Item1.CompareTo(x.Item1);
        }
    }

    private static readonly PathComparer AstarComparer = new();

    private void UpdatePath(Dictionary<EntityUid, GridPathfindingComponent> graphs, PathRequest request)
    {
        // TODO: Need start / resuming.
        // First run
        if (!request.Started)
        {

        }

        DebugTools.Assert(!request.Task.IsCompleted);

        if (request.Start.Equals(request.End))
        {
            request.Tcs.TrySetResult(PathResult.Path);
            return;
        }

        var frontier = new PriorityQueue<ValueTuple<float, PathPoly>>(AstarComparer);
        var costSoFar = new Dictionary<PathPoly, float>();
        var cameFrom = new Dictionary<PathPoly, PathPoly>();
        var stopwatch = new Stopwatch();
        stopwatch.Restart();

        PathPoly? currentNode = null;

        // TODO: Portals + portal flags + storing portals on the comp
        var startGridUid = request.Start.GetGridUid(EntityManager);
        var endGridUid = request.End.GetGridUid(EntityManager);

        if (startGridUid == null || startGridUid != endGridUid)
        {
            request.Tcs.TrySetResult(PathResult.NoPath);
            return;
        }

        var graph = graphs[startGridUid.Value];

        var startNode = GetPoly(request.Start);
        var endNode = GetPoly(request.End);

        if (startNode == null || endNode == null)
        {
            request.Tcs.TrySetResult(PathResult.NoPath);
            return;
        }

        frontier.Add((0.0f, startNode.Value));
        costSoFar[startNode.Value] = 0.0f;
        var count = 0;

        while (frontier.Count > 0)
        {
            // Handle whether we need to pause if we've taken too long
            count++;

            // Suspend
            if (count % 20 == 0 && count > 0 && stopwatch.Elapsed > PathTime)
            {
                return;
            }

            // Actual pathfinding here
            (_, currentNode) = frontier.Take();

            if (currentNode.Equals(endNode))
            {
                break;
            }

            foreach (var neighborRef in currentNode.Value.Neighbors)
            {
                // TODO: Can short-circuit if they're on the same chunk
                var neighbor = graph.GetNeighbor(neighborRef);

                // If tile is untraversable it'll be null
                var tileCost = GetTileCost(request, currentNode.Value, neighbor);

                if (tileCost.Equals(0f))
                {
                    continue;
                }

                // f = g + h
                // gScore is distance to the start node
                // hScore is distance to the end node
                var gScore = costSoFar[currentNode.Value] + tileCost;
                if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                cameFrom[neighbor] = currentNode.Value;
                costSoFar[neighbor] = gScore;
                // pFactor is tie-breaker where the fscore is otherwise equal.
                // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                // There's other ways to do it but future consideration
                // The closer the fScore is to the actual distance then the better the pathfinder will be
                // (i.e. somewhere between 1 and infinite)
                // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                var fScore = gScore + OctileDistance(endNode.Value, neighbor) * (1.0f + 1.0f / 1000.0f);
                frontier.Add((fScore, neighbor));
            }
        }

        if (!endNode.Equals(currentNode))
        {
            request.Tcs.TrySetResult(PathResult.NoPath);
            return;
        }

        var route = ReconstructPath(cameFrom, currentNode.Value);
        request.Polys = route;
        // var simplifiedRoute = Simplify(route, 0f);
        // var actualRoute = new Queue<EntityCoordinates>(simplifiedRoute);
        request.Tcs.TrySetResult(PathResult.Path);
    }

    private Queue<PathPoly> ReconstructPath(Dictionary<PathPoly, PathPoly> path, PathPoly currentNode)
    {
        throw new NotImplementedException();
    }

    private float GetTileCost(PathRequest request, PathPoly start, PathPoly end)
    {
        return 0f;
    }
}
