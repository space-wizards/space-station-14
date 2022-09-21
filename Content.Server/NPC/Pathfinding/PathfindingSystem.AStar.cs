using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    private sealed class PathComparer : IComparer<ValueTuple<float, PathPolyRef>>
    {
        public int Compare((float, PathPolyRef) x, (float, PathPolyRef) y)
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

        // TODO: Cleanup the poly / ref situation
        // TODO: Need partial planning that uses best node.

        // First run
        if (!request.Started)
        {
            request.Frontier = new PriorityQueue<(float, PathPolyRef)>(AstarComparer);
            request.Started = true;
        }
        // TODO: Re-validate state... or something

        DebugTools.Assert(!request.Task.IsCompleted);
        request.Stopwatch.Restart();

        PathPolyRef? currentNode = null;

        // TODO: Portals + portal flags + storing portals on the comp
        var startNodeRef = GetPolyRef(request.Start);
        var endNodeRef = GetPolyRef(request.End);

        if (startNodeRef == null || endNodeRef == null)
        {
            return PathResult.NoPath;
        }

        var startNode = GetPoly(startNodeRef.Value);
        var endNode = GetPoly(endNodeRef.Value);

        if (startNode == null || endNode == null)
        {
            return PathResult.NoPath;
        }

        // TODO: Mixing path nodes and refs has made this spaghet.
        var currentGraph = startNodeRef.Value.GraphUid;

        if (!graphs.TryGetValue(currentGraph, out var comp))
        {
            return PathResult.NoPath;
        }

        currentNode = startNodeRef;
        request.Frontier.Add((0.0f, startNodeRef.Value));
        request.CostSoFar[startNodeRef.Value] = 0.0f;
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

            if (currentNode.Equals(endNodeRef))
            {
                break;
            }

            var node = GetPoly(currentNode.Value);

            if (node == null)
                continue;

            foreach (var neighborRef in node.Value.Neighbors)
            {
                // TODO: Can short-circuit if they're on the same chunk
                PathPoly neighbor;

                if (currentGraph == neighborRef.GraphUid)
                {
                    neighbor = comp.GetNeighbor(neighborRef);
                }
                else if (graphs.TryGetValue(neighborRef.GraphUid, out var neighborComp))
                {
                    neighbor = neighborComp.GetNeighbor(neighborRef);
                    currentGraph = neighborRef.GraphUid;
                    comp = neighborComp;
                }
                else
                {
                    continue;
                }

                var tileCost = GetTileCost(request, node.Value, neighbor);

                if (tileCost.Equals(0f))
                {
                    continue;
                }

                // f = g + h
                // gScore is distance to the start node
                // hScore is distance to the end node
                var gScore = request.CostSoFar[currentNode.Value] + tileCost;
                if (request.CostSoFar.TryGetValue(neighborRef, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                request.CameFrom[neighborRef] = currentNode.Value;
                request.CostSoFar[neighborRef] = gScore;
                // pFactor is tie-breaker where the fscore is otherwise equal.
                // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                // There's other ways to do it but future consideration
                // The closer the fScore is to the actual distance then the better the pathfinder will be
                // (i.e. somewhere between 1 and infinite)
                // Can use hierarchical pathfinder or whatever to improve the heuristic but this is fine for now.
                var hScore = OctileDistance(endNode.Value, neighbor) * (1.0f + 1.0f / 1000.0f);
                var fScore = gScore + hScore;
                request.Frontier.Add((fScore, neighborRef));
            }
        }

        if (!endNodeRef.Equals(currentNode))
        {
            return PathResult.NoPath;
        }

        var route = ReconstructPath(request.CameFrom, currentNode.Value);
        request.Polys = route;
        var path = new Queue<EntityCoordinates>(route.Count);

        foreach (var polyRef in route)
        {
            var node = GetPoly(polyRef);
            if (node == null)
            {
                return PathResult.NoPath;
            }

            path.Enqueue(ToCoordinates(polyRef.GraphUid, node.Value));
        }

        request.Path = path;

        // var simplifiedRoute = Simplify(route, 0f);
        // var actualRoute = new Queue<EntityCoordinates>(simplifiedRoute);
        return PathResult.Path;
    }

    private Queue<PathPolyRef> ReconstructPath(Dictionary<PathPolyRef, PathPolyRef> path, PathPolyRef currentNodeRef)
    {
        var running = new Stack<PathPolyRef>();
        running.Push(currentNodeRef);
        while (path.ContainsKey(currentNodeRef))
        {
            var previousCurrent = currentNodeRef;
            currentNodeRef = path[currentNodeRef];
            path.Remove(previousCurrent);
            running.Push(currentNodeRef);
        }

        var result = new Queue<PathPolyRef>(running);
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

    private EntityCoordinates ToCoordinates(EntityUid uid, PathPoly poly)
    {
        return new EntityCoordinates(uid, poly.Box.Center);
    }
}
