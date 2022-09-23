using Content.Shared.NPC;
using Robust.Shared.Map;
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

        // TODO: Need partial planning that uses best node.
        PathPoly? currentNode = null;

        // First run
        if (!request.Started)
        {
            request.Frontier = new PriorityQueue<(float, PathPoly)>(AstarComparer);
            request.Started = true;
        }
        // Re-validate nodes
        else
        {
            (_, currentNode) = request.Frontier.Peek();

            if (!currentNode.IsValid())
            {
                return PathResult.NoPath;
            }

            // Re-validate parents too.
            if (request.CameFrom.TryGetValue(currentNode, out var parentNode) && !parentNode.IsValid())
            {
                return PathResult.NoPath;
            }
        }

        DebugTools.Assert(!request.Task.IsCompleted);
        request.Stopwatch.Restart();

        var startNode = GetPoly(request.Start);
        var endNode = GetPoly(request.End);

        if (startNode == null || endNode == null)
        {
            return PathResult.NoPath;
        }

        // TODO: Mixing path nodes and refs has made this spaghet.
        var currentGraph = startNode.GraphUid;

        if (!graphs.TryGetValue(currentGraph, out var comp))
        {
            return PathResult.NoPath;
        }

        currentNode = startNode;
        request.Frontier.Add((0.0f, startNode));
        request.CostSoFar[startNode] = 0.0f;
        var count = 0;

        while (request.Frontier.Count > 0)
        {
            // Handle whether we need to pause if we've taken too long
            count++;

            // Suspend
            if (count % 50 == 0 && count > 0 && request.Stopwatch.Elapsed > PathTime)
            {
                return PathResult.Continuing;
            }

            // Actual pathfinding here
            (_, currentNode) = request.Frontier.Take();

            if (currentNode.Equals(endNode))
            {
                break;
            }

            foreach (var neighbor in currentNode.Neighbors)
            {
                var tileCost = GetTileCost(request, currentNode, neighbor);

                if (tileCost.Equals(0f))
                {
                    continue;
                }

                // f = g + h
                // gScore is distance to the start node
                // hScore is distance to the end node
                var gScore = request.CostSoFar[currentNode] + tileCost;
                if (request.CostSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                request.CameFrom[neighbor] = currentNode;
                request.CostSoFar[neighbor] = gScore;
                // pFactor is tie-breaker where the fscore is otherwise equal.
                // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                // There's other ways to do it but future consideration
                // The closer the fScore is to the actual distance then the better the pathfinder will be
                // (i.e. somewhere between 1 and infinite)
                // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                var hScore = OctileDistance(endNode, neighbor) * (1.0f + 1.0f / 1000.0f);
                var fScore = gScore + hScore;
                request.Frontier.Add((fScore, neighbor));
            }
        }

        if (!endNode.Equals(currentNode))
        {
            return PathResult.NoPath;
        }

        var route = ReconstructPath(request.CameFrom, currentNode);
        request.Polys = route;
        var path = new Queue<EntityCoordinates>(route.Count);

        foreach (var node in route)
        {
            // Due to partial planning some nodes may have been invalidated.
            if (!node.IsValid())
            {
                return PathResult.NoPath;
            }

            path.Enqueue(ToCoordinates(node.GraphUid, node));
        }

        request.Path = path;
        return PathResult.Path;
    }

    private Queue<PathPoly> ReconstructPath(Dictionary<PathPoly, PathPoly> path, PathPoly currentNodeRef)
    {
        var running = new Stack<PathPoly>();
        running.Push(currentNodeRef);
        while (path.ContainsKey(currentNodeRef))
        {
            var previousCurrent = currentNodeRef;
            currentNodeRef = path[currentNodeRef];
            path.Remove(previousCurrent);
            running.Push(currentNodeRef);
        }

        var result = new Queue<PathPoly>(running);
        return result;
    }

    private float GetTileCost(PathRequest request, PathPoly start, PathPoly end)
    {
        var modifier = 1f;

        if ((request.CollisionLayer & end.Data.CollisionMask) != 0x0 ||
            (request.CollisionMask & end.Data.CollisionLayer) != 0x0)
        {
            var isDoor = (end.Data.Flags & PathfindingBreadcrumbFlag.Door) != 0x0;
            var isAccess = (end.Data.Flags & PathfindingBreadcrumbFlag.Access) != 0x0;

            // TODO: Handling power + door prying
            // Door we should be able to open
            if (isDoor && !isAccess)
            {
                modifier += 0.5f;
            }
            // Door we can force open one way or another
            else if (isDoor && isAccess && (request.Flags & PathFlags.Prying) != 0x0)
            {
                modifier += 4f;
            }
            else if ((request.Flags & PathFlags.Smashing) != 0x0 && end.Data.Damage > 0f)
            {
                modifier += 7f + end.Data.Damage / 100f;
            }
            else
            {
                return 0f;
            }
        }

        return modifier * OctileDistance(end, start);
    }

    private EntityCoordinates ToCoordinates(EntityUid uid, PathPoly poly)
    {
        return new EntityCoordinates(uid, poly.Box.Center);
    }
}
