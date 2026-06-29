using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared.Electrocution;

[DataDefinition]
public sealed partial class ElectrocutionNode : Node
{
    [DataField("cable")]
    public EntityUid? CableEntity;
    [DataField("node")]
    public string? NodeName;

    public override IEnumerable<Node> GetReachableNodes(
        Entity<TransformComponent> xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        Entity<MapGridComponent>? grid,
        IEntityManager entMan)
    {
        if (CableEntity == null || NodeName == null)
            yield break;

        var _nodeContainer = entMan.System<NodeContainerSystem>();
        if (_nodeContainer.TryGetNode(CableEntity.Value, NodeName, out Node? node))
            yield return node;
    }
}
