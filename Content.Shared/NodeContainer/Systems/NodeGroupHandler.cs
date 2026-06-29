using System.Linq;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.NodeContainer.Systems;

public interface INodeGroupHandler
{
    /// <summary>
    /// Initializes a node group.
    /// </summary>
    /// <param name="group">The node group.</param>
    /// <param name="sourceNode">The source node of the node group.</param>
    void InitializeGroup(INodeGroup group, Node sourceNode);

    /// <summary>
    ///     Called when a node has been removed from this group via deletion of the node.
    /// </summary>
    /// <remarks>
    ///     Note that this always still results in a complete remake of the group later,
    ///     but hooking this method is good for book keeping.
    /// </remarks>
    /// <param name="group">The node group.</param>
    /// <param name="node">The node that was deleted.</param>
    void RemoveNode(INodeGroup group, Node node);

    /// <summary>
    ///     Called to load this newly created group up with new nodes.
    /// </summary>
    /// <param name="group">The node group.</param>
    /// <param name="groupNodes">The new nodes for this group.</param>
    void LoadNodes(INodeGroup group, List<Node> groupNodes);

    /// <summary>
    ///     Called after the nodes in this group have been made into one or more new groups.
    /// </summary>
    /// <remarks>
    ///     Use this to split in-group data such as pipe gas mixtures into newly split nodes.
    /// </remarks>
    /// <param name="group">The node group.</param>
    /// <param name="newGroups">A list of new groups for this group's former nodes.</param>
    void AfterRemake(INodeGroup group, IEnumerable<IGrouping<INodeGroup?, Node>> newGroups);

    string? GetDebugData(INodeGroup group);
}

/// <summary>
/// Represents a system that handles a node group of a specific type.
/// </summary>
/// <typeparam name="T">Type of the handled node group.</typeparam>
public abstract partial class NodeGroupHandler<T> : EntitySystem, INodeGroupHandler where T : class, INodeGroup
{
    [Dependency] protected NodeGroupSystem NodeGroupSys = default!;

    /// <summary>
    /// Registers this handler in the <see cref="NodeGroupSystem"/> by filling in
    /// <see cref="NodeGroupSystem.NodeGroupTypes"/> and <see cref="NodeGroupSystem.NodeGroupHandlers"/>.
    /// </summary>
    public abstract void RegisterHandler();

    protected virtual void InitializeGroup(T group, Node sourceNode) { }

    protected virtual void RemoveNode(T group, Node node) { }

    protected virtual void LoadNodes(T group, List<Node> groupNodes)
    {
        group.Nodes.AddRange(groupNodes);
    }

    protected virtual string? GetDebugData(T group)
    {
        return null;
    }

    protected virtual void AfterRemake(T group, IEnumerable<IGrouping<INodeGroup?, Node>> newGroups) { }

    public void InitializeGroup(INodeGroup group, Node sourceNode)
    {
        InitializeGroup((T) group, sourceNode);
    }

    public void RemoveNode(INodeGroup group, Node node)
    {
        RemoveNode((T) group, node);
    }

    public void LoadNodes(INodeGroup group, List<Node> groupNodes)
    {
        LoadNodes((T) group, groupNodes);
    }

    public void AfterRemake(INodeGroup group, IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        AfterRemake((T) group, newGroups);
    }

    public string? GetDebugData(INodeGroup group)
    {
        return GetDebugData((T) group);
    }
}
