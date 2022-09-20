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

    private void UpdatePath(PathRequest request)
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

        var startNode = GetPoly(request.Start);
        var endNode = GetPoly(request.End);

        if (startNode == null || endNode == null)
        {
            request.Tcs.TrySetResult(PathResult.NoPath);
            return;
        }

        frontier.Add((0.0f, startNode.Value));
        costSoFar[startNode.Value] = 0.0f;
        var routeFound = false;
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
                routeFound = true;
                break;
            }

            foreach (var neighborRef in currentNode.Value.Neighbors)
            {
                

                // If tile is untraversable it'll be null
                var tileCost = PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, nextNode);
                if (tileCost == null)
                {
                    continue;
                }

                // So if we're going NE then that means either N or E needs to be free to actually get there
                var direction = PathfindingHelpers.RelativeDirection(nextNode, currentNode);
                if (!PathfindingHelpers.DirectionTraversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, currentNode, direction))
                {
                    continue;
                }

                // f = g + h
                // gScore is distance to the start node
                // hScore is distance to the end node
                var gScore = costSoFar[currentNode] + tileCost.Value;
                if (costSoFar.TryGetValue(nextNode, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                cameFrom[nextNode] = currentNode;
                costSoFar[nextNode] = gScore;
                // pFactor is tie-breaker where the fscore is otherwise equal.
                // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                // There's other ways to do it but future consideration
                // The closer the fScore is to the actual distance then the better the pathfinder will be
                // (i.e. somewhere between 1 and infinite)
                // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                var fScore = gScore + PathfindingHelpers.OctileDistance(_endNode, nextNode) * (1.0f + 1.0f / 1000.0f);
                frontier.Add((fScore, nextNode));
            }
        }

        if (!routeFound)
        {
            request.Tcs.TrySetResult(PathResult.NoPath);
            return;
        }

        DebugTools.AssertNotNull(currentNode);

        var route = PathfindingHelpers.ReconstructPath(cameFrom, currentNode!);

        if (route.Count == 1)
        {
            request.Tcs.TrySetResult(PathResult.Path);
            return;
        }

        var simplifiedRoute = PathfindingSystem.Simplify(route, 0f);
        var actualRoute = new Queue<TileRef>(simplifiedRoute);

        request.Tcs.TrySetResult(PathResult.Path);
    }
}
