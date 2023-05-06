using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.Spreader;

[NodeGroup(NodeGroupID.Spreader)]
public sealed class SpreaderNodeGroup : BaseNodeGroup
{
    private IEntityManager _entManager = default!;

    /// <inheritdoc/>
    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);
        _entManager = entMan;
    }

    /// <inheritdoc/>
    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        foreach (var neighborNode in node.ReachableNodes)
        {
            if (_entManager.Deleted(neighborNode.Owner))
                continue;

            _entManager.EnsureComponent<EdgeSpreaderComponent>(neighborNode.Owner);
        }
    }
}
