using System.Collections.Generic;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Power.Nodes
{
    /// <summary>
    ///     Type of node that connects to a <see cref="CableNode"/> below it.
    /// </summary>
    [DataDefinition]
    public class CableDeviceNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var gridIndex = grid.TileIndicesFor(Owner.Transform.Coordinates);

            foreach (var node in NodeHelpers.GetNodesInTile(entMan, grid, gridIndex))
            {
                if (node is CableNode)
                    yield return node;
            }
        }
    }
}
