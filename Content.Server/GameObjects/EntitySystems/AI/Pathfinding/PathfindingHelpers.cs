using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
    public static class PathfindingHelpers
    {
        public static bool TryEndNode(ref PathfindingNode endNode, PathfindingArgs pathfindingArgs)
        {
            if (!Traversable(pathfindingArgs.CollisionMask, pathfindingArgs.Access, endNode))
            {
                if (pathfindingArgs.Proximity > 0.0f)
                {
                    // TODO: Should make this account for proximities,
                    // probably some kind of breadth-first search to find a valid one
                    foreach (var (_, node) in endNode.Neighbors)
                    {
                        if (Traversable(pathfindingArgs.CollisionMask, pathfindingArgs.Access, node))
                        {
                            endNode = node;
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        public static bool DirectionTraversable(int collisionMask, ICollection<string> access, PathfindingNode currentNode, Direction direction)
         {
            // If it's a diagonal we need to check NSEW to see if we can get to it and stop corner cutting, NE needs N and E etc.
            // Given there's different collision layers stored for each node in the graph it's probably not worth it to cache this
            // Also this will help with corner-cutting

            currentNode.Neighbors.TryGetValue(Direction.North, out var northNeighbor);
            currentNode.Neighbors.TryGetValue(Direction.South, out var southNeighbor);
            currentNode.Neighbors.TryGetValue(Direction.East, out var eastNeighbor);
            currentNode.Neighbors.TryGetValue(Direction.West, out var westNeighbor);

            switch (direction)
            {
                case Direction.NorthEast:
                    if (northNeighbor == null || eastNeighbor == null) return false;
                    if (!Traversable(collisionMask, access, northNeighbor) ||
                        !Traversable(collisionMask, access, eastNeighbor))
                    {
                        return false;
                    }
                    break;
                case Direction.NorthWest:
                    if (northNeighbor == null || westNeighbor == null) return false;
                    if (!Traversable(collisionMask, access, northNeighbor) ||
                        !Traversable(collisionMask, access, westNeighbor))
                    {
                        return false;
                    }
                    break;
                case Direction.SouthWest:
                    if (southNeighbor == null || westNeighbor == null) return false;
                    if (!Traversable(collisionMask, access, southNeighbor) ||
                        !Traversable(collisionMask, access, westNeighbor))
                    {
                        return false;
                    }
                    break;
                case Direction.SouthEast:
                    if (southNeighbor == null || eastNeighbor == null) return false;
                    if (!Traversable(collisionMask, access, southNeighbor) ||
                        !Traversable(collisionMask, access, eastNeighbor))
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        public static bool Traversable(int collisionMask, ICollection<string> access, PathfindingNode node)
        {
            if ((collisionMask & node.BlockedCollisionMask) != 0)
            {
                return false;
            }

            foreach (var reader in node.AccessReaders)
            {
                if (!reader.IsAllowed(access))
                {
                    return false;
                }
            }

            return true;
        }
        
        public static Queue<TileRef> ReconstructPath(Dictionary<PathfindingNode, PathfindingNode> cameFrom, PathfindingNode current)
        {
            var running = new Stack<TileRef>();
            running.Push(current.TileRef);
            while (cameFrom.ContainsKey(current))
            {
                var previousCurrent = current;
                current = cameFrom[current];
                cameFrom.Remove(previousCurrent);
                running.Push(current.TileRef);
            }

            var result = new Queue<TileRef>(running);

            return result;
        }

        /// <summary>
        /// This will reconstruct the path and fill in the tile holes as well
        /// </summary>
        /// <param name="cameFrom"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public static Queue<TileRef> ReconstructJumpPath(Dictionary<PathfindingNode, PathfindingNode> cameFrom, PathfindingNode current)
        {
            var running = new Stack<TileRef>();
            running.Push(current.TileRef);
            while (cameFrom.ContainsKey(current))
            {
                var previousCurrent = current;
                current = cameFrom[current];
                var intermediate = previousCurrent;
                cameFrom.Remove(previousCurrent);
                var pathfindingSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();
                var mapManager = IoCManager.Resolve<IMapManager>();
                var grid = mapManager.GetGrid(current.TileRef.GridIndex);

                // Get all the intermediate nodes
                while (true)
                {
                    var xOffset = 0;
                    var yOffset = 0;

                    if (intermediate.TileRef.X < current.TileRef.X)
                    {
                        xOffset += 1;
                    }
                    else if (intermediate.TileRef.X > current.TileRef.X)
                    {
                        xOffset -= 1;
                    }
                    else
                    {
                        xOffset = 0;
                    }

                    if (intermediate.TileRef.Y < current.TileRef.Y)
                    {
                        yOffset += 1;
                    }
                    else if (intermediate.TileRef.Y > current.TileRef.Y)
                    {
                        yOffset -= 1;
                    }
                    else
                    {
                        yOffset = 0;
                    }

                    intermediate = pathfindingSystem.GetNode(grid.GetTileRef(
                        new MapIndices(intermediate.TileRef.X + xOffset, intermediate.TileRef.Y + yOffset)));

                    if (intermediate.TileRef != current.TileRef)
                    {
                        // Hacky corner cut fix

                        running.Push(intermediate.TileRef);
                        continue;
                    }

                    break;
                }
                running.Push(current.TileRef);
            }

            var result = new Queue<TileRef>(running);

            return result;
        }

        public static float OctileDistance(PathfindingNode endNode, PathfindingNode currentNode)
        {
            // "Fast Euclidean" / octile.
            // This implementation is written down in a few sources; it just saves doing sqrt.
            int dstX = Math.Abs(currentNode.TileRef.X - endNode.TileRef.X);
            int dstY = Math.Abs(currentNode.TileRef.Y - endNode.TileRef.Y);
            if (dstX > dstY)
            {
                return 1.4f * dstY + (dstX - dstY);
            }

            return 1.4f * dstX + (dstY - dstX);
        }
        
        public static float OctileDistance(TileRef endTile, TileRef startTile)
        {
            // "Fast Euclidean" / octile.
            // This implementation is written down in a few sources; it just saves doing sqrt.
            int dstX = Math.Abs(startTile.X - endTile.X);
            int dstY = Math.Abs(startTile.Y - endTile.Y);
            if (dstX > dstY)
            {
                return 1.4f * dstY + (dstX - dstY);
            }

            return 1.4f * dstX + (dstY - dstX);
        }

        public static float ManhattanDistance(PathfindingNode endNode, PathfindingNode currentNode)
        {
            return Math.Abs(currentNode.TileRef.X - endNode.TileRef.X) + Math.Abs(currentNode.TileRef.Y - endNode.TileRef.Y);
        }

        public static float? GetTileCost(PathfindingArgs pathfindingArgs, PathfindingNode start, PathfindingNode end)
        {
            if (!pathfindingArgs.NoClip && !Traversable(pathfindingArgs.CollisionMask, pathfindingArgs.Access, end))
            {
                return null;
            }

            if (!pathfindingArgs.AllowSpace && end.TileRef.Tile.IsEmpty)
            {
                return null;
            }

            var cost = 1.0f;

            switch (pathfindingArgs.AllowDiagonals)
            {
                case true:
                    cost *= OctileDistance(end, start);
                    break;
                // Manhattan distance
                case false:
                    cost *= ManhattanDistance(end, start);
                    break;
            }

            return cost;
        }
    }
}
