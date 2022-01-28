using System.Collections.Generic;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Power.Nodes
{
    [DataDefinition]
    public class CableTerminalPortNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes()
        {
            if (!Anchored)
                yield break;

            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID == GridId.Invalid)
                yield break; // No funny nodes in spess.

            var entMan = IoCManager.Resolve<IEntityManager>();
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).GridID);
            var gridIndex = grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates);

            var nodes = NodeHelpers.GetCardinalNeighborNodes(entMan, grid, gridIndex, includeSameTile: false);
            foreach (var (_, node) in nodes)
            {
                if (node is CableTerminalNode)
                    yield return node;
            }
        }
    }
}
