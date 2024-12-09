using Robust.Shared.Map.Components;

namespace Content.Server.NodeContainer.Nodes
{
    [DataDefinition]
    public sealed partial class PortPipeNode : PipeNode
    {
        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan,
            SharedMapSystem mapSystem)
        {
            if (!xform.Anchored
                || xform.GridUid == null
                || grid == null)
                yield break;

            var gridIndex = mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, xform, grid, gridIndex, mapSystem))
            {
                if (node is PortablePipeNode)
                    yield return node;
            }

            foreach (var node in base.GetReachableNodes(xform, nodeQuery, xformQuery, grid, entMan, mapSystem))
            {
                yield return node;
            }
        }
    }
}
