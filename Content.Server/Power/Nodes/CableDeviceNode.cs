using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    /// <summary>
    ///     Type of node that connects to a <see cref="CableNode"/> below it.
    /// </summary>
    [DataDefinition]
    [Virtual]
    public class CableDeviceNode : Node
    {
        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan)
        {
            if (!xform.Anchored || grid == null)
                yield break;

            var gridIndex = grid.TileIndicesFor(xform.Coordinates);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, gridIndex))
            {
                if (node is CableNode)
                    yield return node;
            }
        }
    }
}
