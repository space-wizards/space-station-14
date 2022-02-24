using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.AI;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.AI.Pathfinding.Pathfinders
{
    public sealed class AStarPathfindingJob : Job<Queue<TileRef>>
    {
#if DEBUG
        public static event Action<SharedAiDebug.AStarRouteDebug>? DebugRoute;
#endif

        private readonly PathfindingNode? _startNode;
        private PathfindingNode? _endNode;
        private readonly PathfindingArgs _pathfindingArgs;

        public AStarPathfindingJob(
            double maxTime,
            PathfindingNode startNode,
            PathfindingNode endNode,
            PathfindingArgs pathfindingArgs,
            CancellationToken cancellationToken) : base(maxTime, cancellationToken)
        {
            _startNode = startNode;
            _endNode = endNode;
            _pathfindingArgs = pathfindingArgs;
        }

        protected override async Task<Queue<TileRef>?> Process()
        {
            if (_startNode == null ||
                _endNode == null ||
                Status == JobStatus.Finished)
            {
                return null;
            }

            // If we couldn't get a nearby node that's good enough
            if (!PathfindingHelpers.TryEndNode(ref _endNode, _pathfindingArgs))
            {
                return null;
            }

            var frontier = new PriorityQueue<ValueTuple<float, PathfindingNode>>(new PathfindingComparer());
            var costSoFar = new Dictionary<PathfindingNode, float>();
            var cameFrom = new Dictionary<PathfindingNode, PathfindingNode>();

            PathfindingNode? currentNode = null;
            frontier.Add((0.0f, _startNode));
            costSoFar[_startNode] = 0.0f;
            var routeFound = false;
            var count = 0;

            while (frontier.Count > 0)
            {
                // Handle whether we need to pause if we've taken too long
                count++;
                if (count % 20 == 0 && count > 0)
                {
                    await SuspendIfOutOfTime();

                    if (_startNode == null || _endNode == null)
                    {
                        return null;
                    }
                }

                // Actual pathfinding here
                (_, currentNode) = frontier.Take();
                if (currentNode.Equals(_endNode))
                {
                    routeFound = true;
                    break;
                }

                foreach (var nextNode in currentNode.GetNeighbors())
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
                return null;
            }

            DebugTools.AssertNotNull(currentNode);

            var route = PathfindingHelpers.ReconstructPath(cameFrom, currentNode!);

            if (route.Count == 1)
            {
                return null;
            }

#if DEBUG
            // Need to get data into an easier format to send to the relevant clients
            if (DebugRoute != null && route.Count > 0)
            {
                var debugCameFrom = new Dictionary<TileRef, TileRef>(cameFrom.Count);
                var debugGScores = new Dictionary<TileRef, float>(costSoFar.Count);
                foreach (var (node, parent) in cameFrom)
                {
                    debugCameFrom.Add(node.TileRef, parent.TileRef);
                }

                foreach (var (node, score) in costSoFar)
                {
                    debugGScores.Add(node.TileRef, score);
                }

                var debugRoute = new SharedAiDebug.AStarRouteDebug(
                    _pathfindingArgs.Uid,
                    route,
                    debugCameFrom,
                    debugGScores,
                    DebugTime);

                DebugRoute.Invoke(debugRoute);
            }
#endif

            return route;
        }
    }
}
