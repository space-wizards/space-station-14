using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Content.Shared.Pathfinding;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;


namespace Content.Server.GameObjects.Components.Pathfinding
{
    [RegisterComponent]
    public sealed class ServerPathfindingDebugDebugComponent : SharedPathfindingDebugComponent
    {
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case RequestPathfindingGraphMessage _:
#if DEBUG
                    SendGraph();
#endif
                    break;
            }
        }

        private void SendGraph()
        {
            var pathfindingSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();
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

            var message = new PathfindingGraphMessage(result);
            SendNetworkMessage(message);
        }
    }
}
