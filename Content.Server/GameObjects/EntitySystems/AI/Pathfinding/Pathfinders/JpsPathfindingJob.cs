using System;
using System.Collections;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Queues;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Content.Shared.Pathfinding;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders
{
    public class JpsPathfindingJob : Job<Queue<TileRef>>
    {
        public static event Action<JpsRouteDebug> DebugRoute;

        private PathfindingNode _startNode;
        private PathfindingNode _endNode;
        private PathfindingArgs _pathfindingArgs;

        public JpsPathfindingJob(double maxTime,
            PathfindingNode startNode,
            PathfindingNode endNode,
            PathfindingArgs pathfindingArgs) : base(maxTime)
        {
            _startNode = startNode;
            _endNode = endNode;
            _pathfindingArgs = pathfindingArgs;
        }

        public override IEnumerator Process()
        {
            // VERY similar to A*; main difference is with the neighbor tiles you look for jump nodes instead
            if (_startNode == null)
            {
                Finish();
                yield break;
            }

            if (_endNode == null)
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

#if DEBUG
            var jumpNodes = new HashSet<PathfindingNode>();
#endif

            PathfindingNode currentNode = null;
            openTiles.Enqueue(_startNode, 0);
            gScores[_startNode] = 0.0f;
            var routeFound = false;
            var count = 0;

            while (openTiles.Count > 0)
            {
                count++;

                // JPS probably getting a lot fewer nodes than A* is
                if (count % 5 == 0 && count > 0)
                {
                    if (OutOfTime())
                    {
                        yield return null;
                        StopWatch.Restart();
                        Status = Status.Running;
                    }
                }

                currentNode = openTiles.Dequeue();
                if (currentNode.Equals(_endNode))
                {
                    routeFound = true;
                    break;
                }

                foreach (var (direction, _) in currentNode.Neighbors)
                {
                    var jumpNode = GetJumpPoint(_pathfindingArgs.CollisionMask, currentNode, direction, _endNode);

                    if (jumpNode != null && !closedTiles.Contains(jumpNode))
                    {
                        closedTiles.Add(jumpNode);
#if DEBUG
                        jumpNodes.Add(jumpNode);
#endif
                        // GetJumpPoint should already check if we can traverse to the node
                        var tileCost = Utils.GetTileCost(_pathfindingArgs, currentNode, jumpNode);

                        if (tileCost == null)
                        {
                            throw new InvalidOperationException();
                        }

                        var gScore = gScores[currentNode] + tileCost.Value;

                        if (gScores.TryGetValue(jumpNode, out var nextValue) && gScore >= nextValue)
                        {
                            continue;
                        }

                        cameFrom[jumpNode] = currentNode;
                        gScores[jumpNode] = gScore;
                        // pFactor is tie-breaker where the fscore is otherwise equal.
                        // See http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
                        // There's other ways to do it but future consideration
                        var fScore = gScores[jumpNode] + Utils.OctileDistance(_endNode, jumpNode) * (1.0f + 1.0f / 1000.0f);
                        openTiles.Enqueue(jumpNode, fScore);
                    }
                }
            }

            if (!routeFound)
            {
                Finish();
                yield break;
            }

            var route = Utils.ReconstructJumpPath(cameFrom, currentNode);
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
                var debugJumpNodes = new HashSet<TileRef>(jumpNodes.Count);

                foreach (var node in jumpNodes)
                {
                    debugJumpNodes.Add(node.TileRef);
                }

                var debugRoute = new JpsRouteDebug(
                    _pathfindingArgs.Uid,
                    route,
                    debugJumpNodes,
                    DebugTime);

                DebugRoute.Invoke(debugRoute);
            }
#endif

            Result = route;
        }

        private PathfindingNode GetJumpPoint(int collisionMask, PathfindingNode currentNode, Direction direction, PathfindingNode endNode)
        {
            var count = 0;

            while (count < 1000)
            {
                count++;
                var nextNode = currentNode.GetNeighbor(direction);

                // We'll do opposite DirectionTraversable just because of how the method's setup
                // Nodes should be 2-way anyway.
                if (nextNode == null ||
                    Utils.GetTileCost(_pathfindingArgs, currentNode, nextNode) == null)
                {
                    return null;
                }

                if (nextNode == endNode)
                {
                    return endNode;
                }

                // Horizontal and vertical are treated the same i.e.
                // They only check in their specific direction
                // (So Going North means you check NorthWest and NorthEast to see if we're a jump point)

                // Diagonals also check the cardinal directions at the same time at the same time

                // See https://harablog.wordpress.com/2011/09/07/jump-point-search/ for original description
                switch (direction)
                {
                    case Direction.East:
                        if (IsCardinalJumpPoint(collisionMask, direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.NorthEast:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(collisionMask, nextNode, Direction.North, endNode) != null || GetJumpPoint(collisionMask, nextNode, Direction.East, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.North:
                        if (IsCardinalJumpPoint(collisionMask, direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.NorthWest:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(collisionMask, nextNode, Direction.North, endNode) != null || GetJumpPoint(collisionMask, nextNode, Direction.West, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.West:
                        if (IsCardinalJumpPoint(collisionMask, direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.SouthWest:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(collisionMask, nextNode, Direction.South, endNode) != null || GetJumpPoint(collisionMask, nextNode, Direction.West, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.South:
                        if (IsCardinalJumpPoint(collisionMask, direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.SouthEast:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(collisionMask, nextNode, Direction.South, endNode) != null || GetJumpPoint(collisionMask, nextNode, Direction.East, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                currentNode = nextNode;
            }

            Logger.WarningS("pathfinding", "Recursion found in JPS pathfinder");
            return null;
        }

        private bool IsDiagonalJumpPoint(Direction direction, PathfindingNode currentNode)
        {
            // If we're going diagonally need to check all cardinals. Maybe could do this in a loop? My brain no worky right now.
            // From NorthEast we check North, East, SouthEast
            // So if South closed we also check SouthEast (i.e. same x-direction of travel).

            var openNeighborOne = currentNode.GetNeighbor((Direction) ((int) direction + 1 % 8));
            var closedNeighborOne = currentNode.GetNeighbor((Direction) ((int) direction + 2 % 8));

            if ((closedNeighborOne == null || Utils.GetTileCost(_pathfindingArgs, currentNode, closedNeighborOne) == null)
                && openNeighborOne != null && Utils.GetTileCost(_pathfindingArgs, currentNode, openNeighborOne) != null)
            {
                return true;
            }

            var openNeighborTwo = currentNode.GetNeighbor((Direction) ((int) direction + 5 % 8));
            var closedNeighborTwo = currentNode.GetNeighbor((Direction) ((int) direction + 6 % 8));

            if ((closedNeighborTwo == null || Utils.GetTileCost(_pathfindingArgs, currentNode, closedNeighborTwo) == null)
                && openNeighborTwo != null && Utils.GetTileCost(_pathfindingArgs, currentNode, openNeighborTwo) != null)
            {
                return true;
            }
            /* I don't think we need this?
            var openNeighborThree = currentNode.GetNeighbor((Direction) ((int) direction + 7 % 8));
            var closedNeighborThree = currentNode.GetNeighbor((Direction) ((int) direction + 6 % 8));

            if ((closedNeighborThree == null || !Utils.Traversable(collisionMask, closedNeighborThree.CollisionMask)) &&
                (openNeighborThree != null && Utils.Traversable(collisionMask, openNeighborThree.CollisionMask)))
            {
                return true;
            }
            */

            var openNeighborFour = currentNode.GetNeighbor((Direction) ((int) direction + 2 % 8));
            var closedNeighborFour = currentNode.GetNeighbor((Direction) ((int) direction + 3 % 8));

            if ((closedNeighborFour == null || Utils.GetTileCost(_pathfindingArgs, currentNode, closedNeighborFour) == null)
                && openNeighborFour != null && Utils.GetTileCost(_pathfindingArgs, currentNode, openNeighborFour) != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check to see if the node is a jump point (only works for cardinal directions)
        /// </summary>
        private static bool IsCardinalJumpPoint(int collisionMask, Direction direction, PathfindingNode currentNode)
        {
            // If we're going east we need North and NorthEast / South and SouthEast, etc. for each variant
            var openNeighborOne = currentNode.GetNeighbor((Direction) ((int) direction + 1 % 8));
            var closedNeighborOne = currentNode.GetNeighbor((Direction) ((int) direction + 2 % 8));

            if ((closedNeighborOne == null || !Utils.Traversable(collisionMask, closedNeighborOne.CollisionMask)) &&
                (openNeighborOne != null && Utils.Traversable(collisionMask, openNeighborOne.CollisionMask)))
            {
                return true;
            }

            var openNeighborTwo = currentNode.GetNeighbor((Direction) ((int) direction - 1 % 8));
            var closedNeighborTwo = currentNode.GetNeighbor((Direction) ((int) direction - 2 % 8));

            if ((closedNeighborTwo == null || !Utils.Traversable(collisionMask, closedNeighborTwo.CollisionMask)) &&
                (openNeighborTwo != null && Utils.Traversable(collisionMask, openNeighborTwo.CollisionMask)))
            {
                return true;
            }

            return false;
        }
    }
}
