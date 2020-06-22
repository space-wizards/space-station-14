using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Content.Shared.AI;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders
{
    public class AStarPathfindingJob : Job<Queue<TileRef>>
    {
        public static event Action<SharedAiDebug.AStarRouteDebug> DebugRoute;

        private PathfindingNode _startNode;
        private PathfindingNode _endNode;
        private PathfindingArgs _pathfindingArgs;

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

        protected override async Task<Queue<TileRef>> Process()
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

            var openTiles = new PriorityQueue<ValueTuple<float, PathfindingNode>>(new PathfindingComparer());
            var gScores = new Dictionary<PathfindingNode, float>();
            var cameFrom = new Dictionary<PathfindingNode, PathfindingNode>();
            var closedTiles = new HashSet<PathfindingNode>();

            PathfindingNode currentNode = null;
            openTiles.Add((0.0f, _startNode));
            gScores[_startNode] = 0.0f;
            var routeFound = false;
            var count = 0;

            while (openTiles.Count > 0)
            {
                count++;

                if (count % 20 == 0 && count > 0)
                {
                    await SuspendIfOutOfTime();
                }

                if (_startNode == null || _endNode == null)
                {
                    return null;
                }

                (_, currentNode) = openTiles.Take();
                if (currentNode.Equals(_endNode))
                {
                    routeFound = true;
                    break;
                }

                closedTiles.Add(currentNode);

                foreach (var (direction, nextNode) in currentNode.Neighbors)
                {
                    if (closedTiles.Contains(nextNode))
                    {
                        continue;
                    }

                    // If tile is untraversable it'll be null
                    var tileCost = PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, nextNode);

                    if (tileCost == null || !PathfindingHelpers.DirectionTraversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, currentNode, direction))
                    {
                        continue;
                    }

                    var gScore = gScores[currentNode] + tileCost.Value;

                    if (gScores.TryGetValue(nextNode, out var nextValue) && gScore >= nextValue)
                    {
                        continue;
                    }

                    cameFrom[nextNode] = currentNode;
                    gScores[nextNode] = gScore;
                    // pFactor is tie-breaker where the fscore is otherwise equal.
                    // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                    // There's other ways to do it but future consideration
                    var fScore = gScores[nextNode] + PathfindingHelpers.OctileDistance(_endNode, nextNode) * (1.0f + 1.0f / 1000.0f);
                    openTiles.Add((fScore, nextNode));
                }
            }

            if (!routeFound)
            {
                return null;
            }

            var route = PathfindingHelpers.ReconstructPath(cameFrom, currentNode);

            if (route.Count == 1)
            {
                return null;
            }

#if DEBUG
            // Need to get data into an easier format to send to the relevant clients
            if (DebugRoute != null && route.Count > 0)
            {
                var debugCameFrom = new Dictionary<TileRef, TileRef>(cameFrom.Count);
                var debugGScores = new Dictionary<TileRef, float>(gScores.Count);
                var debugClosedTiles = new HashSet<TileRef>(closedTiles.Count);

                foreach (var (node, parent) in cameFrom)
                {
                    debugCameFrom.Add(node.TileRef, parent.TileRef);
                }

                foreach (var (node, score) in gScores)
                {
                    debugGScores.Add(node.TileRef, score);
                }

                foreach (var node in closedTiles)
                {
                    debugClosedTiles.Add(node.TileRef);
                }

                var debugRoute = new SharedAiDebug.AStarRouteDebug(
                    _pathfindingArgs.Uid,
                    route,
                    debugCameFrom,
                    debugGScores,
                    debugClosedTiles,
                    DebugTime);

                DebugRoute.Invoke(debugRoute);
            }
#endif

            return route;
        }
    }
}
