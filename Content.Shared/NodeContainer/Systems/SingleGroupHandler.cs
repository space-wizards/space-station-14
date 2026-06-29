using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.NodeContainer.Systems;

/// <summary>
/// A variant of <see cref="NodeGroupHandler{T}"/> that automatically registers the handler and the node group.
/// </summary>
/// <typeparam name="T">Type of the handled node group.</typeparam>
public abstract partial class SingleNodeGroupHandler<T> : NodeGroupHandler<T> where T : class, INodeGroup
{
    protected Type NodeGroupType => typeof(T);
    protected abstract NodeGroupID NodeGroupID { get; }

    public override void RegisterHandler()
    {
        NodeGroupSys.NodeGroupTypes.Add(NodeGroupID, NodeGroupType);
        NodeGroupSys.NodeGroupHandlers.Add(NodeGroupType, this);
    }
}
