using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Shared.AI;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
#if DEBUG
    [UsedImplicitly]
    public class ServerPathfindingDebugSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            AStarPathfindingJob.DebugRoute += DispatchAStarDebug;
            JpsPathfindingJob.DebugRoute += DispatchJpsDebug;
            SubscribeNetworkEvent<SharedAiDebug.RequestPathfindingGraphMessage>(DispatchGraph);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            AStarPathfindingJob.DebugRoute -= DispatchAStarDebug;
            JpsPathfindingJob.DebugRoute -= DispatchJpsDebug;
        }

        private void DispatchAStarDebug(SharedAiDebug.AStarRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(tileGrid.ToMapPos(EntityManager));
            }

            var cameFrom = new Dictionary<Vector2, Vector2>();
            foreach (var (from, to) in routeDebug.CameFrom)
            {
                var tileOneGrid = mapManager.GetGrid(from.GridIndex).GridTileToLocal(from.GridIndices);
                var tileOneWorld = tileOneGrid.ToMapPos(EntityManager);
                var tileTwoGrid = mapManager.GetGrid(to.GridIndex).GridTileToLocal(to.GridIndices);
                var tileTwoWorld = tileTwoGrid.ToMapPos(EntityManager);
                cameFrom.Add(tileOneWorld, tileTwoWorld);
            }

            var gScores = new Dictionary<Vector2, float>();
            foreach (var (tile, score) in routeDebug.GScores)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                gScores.Add(tileGrid.ToMapPos(EntityManager), score);
            }

            var systemMessage = new SharedAiDebug.AStarRouteMessage(
                routeDebug.EntityUid,
                route,
                cameFrom,
                gScores,
                routeDebug.TimeTaken
                );

            RaiseNetworkEvent(systemMessage);
        }

        private void DispatchJpsDebug(SharedAiDebug.JpsRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(tileGrid.ToMapPos(EntityManager));
            }

            var jumpNodes = new List<Vector2>();
            foreach (var tile in routeDebug.JumpNodes)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                jumpNodes.Add(tileGrid.ToMapPos(EntityManager));
            }

            var systemMessage = new SharedAiDebug.JpsRouteMessage(
                routeDebug.EntityUid,
                route,
                jumpNodes,
                routeDebug.TimeTaken
            );

            RaiseNetworkEvent(systemMessage);
        }

        private void DispatchGraph(SharedAiDebug.RequestPathfindingGraphMessage message)
        {
            var pathfindingSystem = EntitySystemManager.GetEntitySystem<PathfindingSystem>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            var result = new Dictionary<int, List<Vector2>>();

            var idx = 0;

            foreach (var (gridId, chunks) in pathfindingSystem.Graph)
            {
                var gridManager = mapManager.GetGrid(gridId);

                foreach (var chunk in chunks.Values)
                {
                    var nodes = new List<Vector2>();
                    foreach (var node in chunk.Nodes)
                    {
                        var worldTile = gridManager.GridTileToWorldPos(node.TileRef.GridIndices);

                        nodes.Add(worldTile);
                    }

                    result.Add(idx, nodes);
                    idx++;
                }
            }

            var systemMessage = new SharedAiDebug.PathfindingGraphMessage(result);
            RaiseNetworkEvent(systemMessage);
        }
    }
#endif
}
