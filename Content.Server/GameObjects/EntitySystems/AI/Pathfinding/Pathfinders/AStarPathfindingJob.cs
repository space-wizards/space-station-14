using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Queues;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Content.Shared.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders
{
    public class AStarPathfindingJob : Job<Queue<TileRef>>
    {
        public static event Action<AStarRouteDebug> DebugRoute;

        private PathfindingNode _startNode;
        private PathfindingNode _endNode;
        private PathfindingArgs _pathfindingArgs;
        private CancellationTokenSource _cancellationToken;

        public AStarPathfindingJob(
            double maxTime,
            PathfindingNode startNode,
            PathfindingNode endNode,
            PathfindingArgs pathfindingArgs,
            CancellationTokenSource cancellationToken = null) : base(maxTime)
        {
            _startNode = startNode;
            _endNode = endNode;
            _pathfindingArgs = pathfindingArgs;
            _cancellationToken = cancellationToken;
        }

        public override IEnumerator Process()
        {
            if ((_cancellationToken != null && _cancellationToken.IsCancellationRequested) ||
                _startNode == null ||
                _endNode == null ||
                Status == Status.Finished)
            {
                Finish();
                yield break;
            }

            // If we couldn't get a nearby node that's good enough
            if (!Utils.TryEndNode(ref _endNode, _pathfindingArgs))
            {
                Finish();
                yield break;
            }

            var openTiles = new PathfindingPriorityQueue<PathfindingNode>();
            var gScores = new Dictionary<PathfindingNode, float>();
            var cameFrom = new Dictionary<PathfindingNode, PathfindingNode>();
            var closedTiles = new HashSet<PathfindingNode>();

            PathfindingNode currentNode = null;
            openTiles.Enqueue(_startNode, 0);
            gScores[_startNode] = 0.0f;
            var routeFound = false;
            var count = 0;

            while (openTiles.Count > 0)
            {
                count++;

                if (count % 20 == 0 && count > 0)
                {
                    if (OutOfTime())
                    {
                        yield return null;
                        if (_cancellationToken != null && _cancellationToken.IsCancellationRequested)
                        {
                            Finish();
                            yield break;
                        }
                        StopWatch.Restart();
                        Status = Status.Running;
                    }
                }

                if (_startNode == null || _endNode == null)
                {
                    Finish();
                    yield break;
                }

                currentNode = openTiles.Dequeue();
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
                    var tileCost = Utils.GetTileCost(_pathfindingArgs, currentNode, nextNode);

                    if (tileCost == null || !Utils.DirectionTraversable(_pathfindingArgs.CollisionMask, currentNode, direction))
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
                    var fScore = gScores[nextNode] + Utils.OctileDistance(_endNode, nextNode) * (1.0f + 1.0f / 1000.0f);
                    openTiles.Enqueue(nextNode, fScore);
                }
            }

            if (!routeFound)
            {
                Finish();
                yield break;
            }

            var route = Utils.ReconstructPath(cameFrom, currentNode);

            if (route.Count == 1)
            {
                Finish();
                yield break;
            }

            Finish();

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

                var debugRoute = new AStarRouteDebug(
                    _pathfindingArgs.Uid,
                    route,
                    debugCameFrom,
                    debugGScores,
                    debugClosedTiles,
                    DebugTime);

                DebugRoute.Invoke(debugRoute);
            }
#endif

            Result = route;
        }
    }
}
