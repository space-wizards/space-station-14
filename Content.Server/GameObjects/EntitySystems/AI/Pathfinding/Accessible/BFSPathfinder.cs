using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Robust.Shared.GameObjects.Systems;
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
        /// If you want Dikstra then add distances
        /// <param name="pathfindingArgs"></param>
        /// <param name="range"></param>
        /// <param name="fromStart">Whether we traverse from the starting tile or the end tile</param>
        /// <returns></returns>
        public static IEnumerable<PathfindingNode> GetTilesInRange(PathfindingArgs pathfindingArgs, float range, bool fromStart = true)
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
                    if (tileCost == null || tileCost > range) continue;
                    
                    openTiles.Enqueue(neighbor);
                    yield return neighbor;
                }
            }
        }
    }
}