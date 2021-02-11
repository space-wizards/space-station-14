using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Accessible
{
    /// <summary>
    /// The simplest pathfinder
    /// </summary>
    public sealed class BFSPathfinder
    {
        /// <summary>
        /// Gets all of the tiles in range that can we access
        /// </summary>
        /// If you want Dikstra then add distances.
        /// Doesn't use the JobQueue as it will generally be encapsulated by other jobs
        /// <param name="pathfindingArgs"></param>
        /// <param name="range"></param>
        /// <param name="fromStart">Whether we traverse from the starting tile or the end tile</param>
        /// <returns></returns>
        public static IEnumerable<PathfindingNode> GetNodesInRange(PathfindingArgs pathfindingArgs, bool fromStart = true)
        {
            var pathfindingSystem = EntitySystem.Get<PathfindingSystem>();
            // Don't need a priority queue given not looking for shortest path
            var openTiles = new Queue<PathfindingNode>();
            var closedTiles = new HashSet<TileRef>();
            PathfindingNode startNode;

            if (fromStart)
            {
                startNode = pathfindingSystem.GetNode(pathfindingArgs.Start);
            }
            else
            {
                startNode = pathfindingSystem.GetNode(pathfindingArgs.End);
            }

            PathfindingNode currentNode;
            openTiles.Enqueue(startNode);

            while (openTiles.Count > 0)
            {
                currentNode = openTiles.Dequeue();

                foreach (var neighbor in currentNode.GetNeighbors())
                {
                    // No distances stored so can just check closed tiles here
                    if (closedTiles.Contains(neighbor.TileRef)) continue;
                    closedTiles.Add(currentNode.TileRef);

                    // So currently tileCost gets the octile distance between the 2 so we'll also use that for our range check
                    var tileCost = PathfindingHelpers.GetTileCost(pathfindingArgs, startNode, neighbor);
                    var direction = PathfindingHelpers.RelativeDirection(neighbor, currentNode);

                    if (tileCost == null ||
                        tileCost > pathfindingArgs.Proximity ||
                        !PathfindingHelpers.DirectionTraversable(pathfindingArgs.CollisionMask, pathfindingArgs.Access, currentNode, direction))
                    {
                        continue;
                    }

                    openTiles.Enqueue(neighbor);
                    yield return neighbor;
                }
            }
        }
    }
}
