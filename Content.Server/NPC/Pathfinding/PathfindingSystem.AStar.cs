using Content.Shared.NPC;
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

    private PathResult UpdatePath(Dictionary<EntityUid, GridPathfindingComponent> graphs, PathRequest request)
    {
        if (request.Start.Equals(request.End))
        {
            return PathResult.Path;
        }

        if (request.Task.IsCanceled)
        {
            return PathResult.NoPath;
        }

        // First run
        if (!request.Started)
        {
            request.Frontier = new PriorityQueue<(float, PathPoly)>(AstarComparer);
            request.Started = true;
        }
        // TODO: Re-validate state... or something

        DebugTools.Assert(!request.Task.IsCompleted);
        request.Stopwatch.Restart();

        PathPoly? currentNode = null;

        // TODO: Portals + portal flags + storing portals on the comp
        var startGridUid = request.Start.GetGridUid(EntityManager);
        var endGridUid = request.End.GetGridUid(EntityManager);

        if (startGridUid == null || startGridUid != endGridUid)
        {
            return PathResult.NoPath;
        }

        if (!graphs.TryGetValue(startGridUid.Value, out var graph))
        {
            return PathResult.NoPath;
        }

        var startNode = GetPoly(request.Start);
        var endNode = GetPoly(request.End);

        if (startNode == null || endNode == null)
        {
            return PathResult.NoPath;
        }

        request.Frontier.Add((0.0f, startNode.Value));
        request.CostSoFar[startNode.Value] = 0.0f;
        var count = 0;

        while (request.Frontier.Count > 0)
        {
            // Handle whether we need to pause if we've taken too long
            count++;

            // Suspend
            if (count % 20 == 0 && count > 0 && request.Stopwatch.Elapsed > PathTime)
            {
                return PathResult.Continuing;
            }

            // Actual pathfinding here
            (_, currentNode) = request.Frontier.Take();

            if (currentNode.Equals(endNode))
            {
                break;
            }

            foreach (var neighborRef in currentNode.Value.Neighbors)
            {
                // TODO: Can short-circuit if they're on the same chunk
                var neighbor = graph.GetNeighbor(neighborRef);

                var tileCost = GetTileCost(request, currentNode.Value, neighbor);

                if (tileCost.Equals(0f))
                {
                    continue;
                }

                // f = g + h
                // gScore is distance to the start node
                // hScore is distance to the end node
                var gScore = request.CostSoFar[currentNode.Value] + tileCost;
                if (request.CostSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                request.CameFrom[neighbor] = currentNode.Value;
                request.CostSoFar[neighbor] = gScore;
                // pFactor is tie-breaker where the fscore is otherwise equal.
                // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                // There's other ways to do it but future consideration
                // The closer the fScore is to the actual distance then the better the pathfinder will be
                // (i.e. somewhere between 1 and infinite)
                // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                var hScore = OctileDistance(endNode.Value, neighbor) * (1.0f + 1.0f / 1000.0f);
                var fScore = gScore + hScore;
                request.Frontier.Add((fScore, neighbor));
            }
        }

        if (!endNode.Equals(currentNode))
        {
            return PathResult.NoPath;
        }

        var route = ReconstructPath(request.CameFrom, currentNode.Value);
        request.Polys = route;
        // var simplifiedRoute = Simplify(route, 0f);
        // var actualRoute = new Queue<EntityCoordinates>(simplifiedRoute);
        return PathResult.Path;
    }

    private Queue<PathPoly> ReconstructPath(Dictionary<PathPoly, PathPoly> path, PathPoly currentNode)
    {
        var running = new Stack<PathPoly>();
        running.Push(currentNode);
        while (path.ContainsKey(currentNode))
        {
            var previousCurrent = currentNode;
            currentNode = path[currentNode];
            path.Remove(previousCurrent);
            running.Push(currentNode);
        }

        var result = new Queue<PathPoly>(running);

        return result;
    }

    private float GetTileCost(PathRequest request, PathPoly start, PathPoly end)
    {
        var modifier = 0f;

        if ((request.CollisionLayer & end.Data.CollisionMask) != 0x0)
        {
            return modifier;
        }

        modifier = 1f;

        return modifier * OctileDistance(end, start);
    }
}
