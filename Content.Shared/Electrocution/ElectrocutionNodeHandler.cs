using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Electrocution;

public sealed partial class ElectrocutionNodeHandler : NodeHandler<ElectrocutionNode>
{
    [Dependency] private NodeContainerSystem _nodeContainer = default!;

    protected override IEnumerable<Node> GetReachableNodes(
        ElectrocutionNode node,
        Entity<TransformComponent> xform,
        Entity<MapGridComponent>? grid)
    {
        if (node.CableEntity == null || node.NodeName == null)
            yield break;

        if (_nodeContainer.TryGetNode(node.CableEntity.Value, node.NodeName, out Node? cableNode))
            yield return cableNode;
    }
}
