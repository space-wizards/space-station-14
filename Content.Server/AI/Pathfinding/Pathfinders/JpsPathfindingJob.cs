using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.AI;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.AI.Pathfinding.Pathfinders
{
    public sealed class JpsPathfindingJob : Job<Queue<TileRef>>
    {
        // Some of this is probably fugly due to other structural changes in pathfinding so it could do with optimisation
        // Realistically it's probably not getting used given it doesn't support tile costs which can be very useful
#if DEBUG
        public static event Action<SharedAiDebug.JpsRouteDebug>? DebugRoute;
#endif

        private readonly PathfindingNode? _startNode;
        private PathfindingNode? _endNode;
        private readonly PathfindingArgs _pathfindingArgs;

        public JpsPathfindingJob(double maxTime,
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
            // VERY similar to A*; main difference is with the neighbor tiles you look for jump nodes instead
            if (_startNode == null ||
                _endNode == null)
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

#if DEBUG
            var jumpNodes = new HashSet<PathfindingNode>();
#endif

            PathfindingNode? currentNode = null;
            openTiles.Add((0, _startNode));
            gScores[_startNode] = 0.0f;
            var routeFound = false;
            var count = 0;

            while (openTiles.Count > 0)
            {
                count++;

                // JPS probably getting a lot fewer nodes than A* is
                if (count % 5 == 0 && count > 0)
                {
                    await SuspendIfOutOfTime();
                }

                (_, currentNode) = openTiles.Take();
                if (currentNode.Equals(_endNode))
                {
                    routeFound = true;
                    break;
                }

                foreach (var node in currentNode.GetNeighbors())
                {
                    var direction = PathfindingHelpers.RelativeDirection(node, currentNode);
                    var jumpNode = GetJumpPoint(currentNode, direction, _endNode);

                    if (jumpNode != null && !closedTiles.Contains(jumpNode))
                    {
                        closedTiles.Add(jumpNode);
#if DEBUG
                        jumpNodes.Add(jumpNode);
#endif
                        // GetJumpPoint should already check if we can traverse to the node
                        var tileCost = PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, jumpNode);

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
                        var fScore = gScores[jumpNode] + PathfindingHelpers.OctileDistance(_endNode, jumpNode) * (1.0f + 1.0f / 1000.0f);
                        openTiles.Add((fScore, jumpNode));
                    }
                }
            }

            if (!routeFound)
            {
                return null;
            }

            DebugTools.AssertNotNull(currentNode);

            var route = PathfindingHelpers.ReconstructJumpPath(cameFrom, currentNode!);

            if (route.Count == 1)
            {
                return null;
            }

#if DEBUG
            // Need to get data into an easier format to send to the relevant clients
            if (DebugRoute != null && route.Count > 0)
            {
                var debugJumpNodes = new HashSet<TileRef>(jumpNodes.Count);

                foreach (var node in jumpNodes)
                {
                    debugJumpNodes.Add(node.TileRef);
                }

                var debugRoute = new SharedAiDebug.JpsRouteDebug(
                    _pathfindingArgs.Uid,
                    route,
                    debugJumpNodes,
                    DebugTime);

                DebugRoute.Invoke(debugRoute);
            }
#endif

            return route;
        }

        private PathfindingNode? GetJumpPoint(PathfindingNode currentNode, Direction direction, PathfindingNode endNode)
        {
            var count = 0;

            while (count < 1000)
            {
                count++;
                PathfindingNode? nextNode = null;
                foreach (var node in currentNode.GetNeighbors())
                {
                    if (PathfindingHelpers.RelativeDirection(node, currentNode) == direction)
                    {
                        nextNode = node;
                        break;
                    }
                }

                // We'll do opposite DirectionTraversable just because of how the method's setup
                // Nodes should be 2-way anyway.
                if (nextNode == null ||
                    PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, nextNode) == null)
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
                        if (IsCardinalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.NorthEast:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(nextNode, Direction.North, endNode) != null || GetJumpPoint(nextNode, Direction.East, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.North:
                        if (IsCardinalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.NorthWest:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(nextNode, Direction.North, endNode) != null || GetJumpPoint(nextNode, Direction.West, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.West:
                        if (IsCardinalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.SouthWest:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(nextNode, Direction.South, endNode) != null || GetJumpPoint(nextNode, Direction.West, endNode) != null)
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.South:
                        if (IsCardinalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        break;
                    case Direction.SouthEast:
                        if (IsDiagonalJumpPoint(direction, nextNode))
                        {
                            return nextNode;
                        }

                        if (GetJumpPoint(nextNode, Direction.South, endNode) != null || GetJumpPoint(nextNode, Direction.East, endNode) != null)
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
            // If we're going diagonally need to check all cardinals.
            // I tried just casting direction ints and offsets to make it smaller but brain no worky.
            // From NorthEast we check (Closed / Open) S - SE, W - NW

            PathfindingNode? openNeighborOne = null;
            PathfindingNode? closedNeighborOne = null;
            PathfindingNode? openNeighborTwo = null;
            PathfindingNode? closedNeighborTwo = null;

            switch (direction)
            {
                case Direction.NorthEast:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.SouthEast:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.South:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.NorthWest:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.West:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.SouthEast:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.NorthEast:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.North:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.SouthWest:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.West:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.SouthWest:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.NorthWest:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.North:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.SouthEast:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.East:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.NorthWest:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.SouthWest:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.South:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.NorthEast:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.East:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if ((closedNeighborOne == null || PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, closedNeighborOne) == null)
                && openNeighborOne != null && PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, openNeighborOne) != null)
            {
                return true;
            }

            if ((closedNeighborTwo == null || PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, closedNeighborTwo) == null)
                && openNeighborTwo != null && PathfindingHelpers.GetTileCost(_pathfindingArgs, currentNode, openNeighborTwo) != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check to see if the node is a jump point (only works for cardinal directions)
        /// </summary>
        private bool IsCardinalJumpPoint(Direction direction, PathfindingNode currentNode)
        {
            PathfindingNode? openNeighborOne = null;
            PathfindingNode? closedNeighborOne = null;
            PathfindingNode? openNeighborTwo = null;
            PathfindingNode? closedNeighborTwo = null;

            switch (direction)
            {
                case Direction.North:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.NorthEast:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.East:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.NorthWest:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.West:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.East:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.NorthEast:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.North:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.SouthEast:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.South:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.South:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.SouthEast:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.East:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.SouthWest:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.West:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                case Direction.West:
                    foreach (var neighbor in currentNode.GetNeighbors())
                    {
                        var neighborDirection = PathfindingHelpers.RelativeDirection(neighbor, currentNode);
                        switch (neighborDirection)
                        {
                            case Direction.NorthWest:
                                openNeighborOne = neighbor;
                                break;
                            case Direction.North:
                                closedNeighborOne = neighbor;
                                break;
                            case Direction.SouthWest:
                                openNeighborTwo = neighbor;
                                break;
                            case Direction.South:
                                closedNeighborTwo = neighbor;
                                break;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if ((closedNeighborOne == null || !PathfindingHelpers.Traversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, closedNeighborOne)) &&
                openNeighborOne != null && PathfindingHelpers.Traversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, openNeighborOne))
            {
                return true;
            }

            if ((closedNeighborTwo == null || !PathfindingHelpers.Traversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, closedNeighborTwo)) &&
                openNeighborTwo != null && PathfindingHelpers.Traversable(_pathfindingArgs.CollisionMask, _pathfindingArgs.Access, openNeighborTwo))
            {
                return true;
            }

            return false;
        }
    }
}
