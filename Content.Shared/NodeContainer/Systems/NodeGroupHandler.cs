using System.Linq;
using Content.Shared.NodeContainer.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.NodeContainer.Systems;

public interface INodeGroupHandler
{
    /// <summary>
    /// Initializes a node group.
    /// </summary>
    /// <param name="group">The node group.</param>
    /// <param name="sourceNode">The source node of the node group.</param>
    void InitializeGroup(Entity<NodeGroupComponent> group, Node sourceNode);

    /// <summary>
    ///     Called when a node has been removed from this group via deletion of the node.
    /// </summary>
    /// <remarks>
    ///     Note that this always still results in a complete remake of the group later,
    ///     but hooking this method is good for book keeping.
    /// </remarks>
    /// <param name="group">The node group.</param>
    /// <param name="node">The node that was deleted.</param>
    void RemoveNode(Entity<NodeGroupComponent> group, Node node);

    /// <summary>
    ///     Called to load this newly created group up with new nodes.
    /// </summary>
    /// <param name="group">The node group.</param>
    /// <param name="groupNodes">The new nodes for this group.</param>
    void LoadNodes(Entity<NodeGroupComponent> group, List<Node> groupNodes);

    /// <summary>
    ///     Called after the nodes in this group have been made into one or more new groups.
    /// </summary>
    /// <remarks>
    ///     Use this to split in-group data such as pipe gas mixtures into newly split nodes.
    /// </remarks>
    /// <param name="group">The node group.</param>
    /// <param name="newGroups">A list of new groups for this group's former nodes.</param>
    void AfterRemake(Entity<NodeGroupComponent> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups);

    string? GetDebugData(Entity<NodeGroupComponent> group);
}

/// <summary>
/// Represents a system that handles a node group of a specific type.
/// </summary>
/// <typeparam name="T">Type of the handled node group.</typeparam>
public abstract partial class NodeGroupHandler<T> : EntitySystem, INodeGroupHandler where T : class, IComponent
{
    [Dependency] protected NodeGroupSystem NodeGroupSys = default!;
    [Dependency] protected EntityQuery<T> Query = default!;

    protected Type NodeGroupCompType => typeof(T);

    public override void Initialize()
    {
        base.Initialize();
        RegisterHandler();
    }

    /// <summary>
    /// Registers this handler in the <see cref="NodeGroupSystem"/> by filling in <see cref="NodeGroupSystem.NodeGroupHandlers"/>.
    /// </summary>
    public abstract void RegisterHandler();

    protected virtual void InitializeGroup(Entity<NodeGroupComponent, T> group, Node sourceNode) { }

    protected virtual void RemoveNode(Entity<NodeGroupComponent, T> group, Node node) { }

    protected virtual void LoadNodes(Entity<NodeGroupComponent, T> group, List<Node> groupNodes)
    {
        group.Comp1.Nodes.AddRange(groupNodes);
    }

    protected virtual string? GetDebugData(Entity<NodeGroupComponent, T> group)
    {
        return null;
    }

    protected virtual void AfterRemake(Entity<NodeGroupComponent, T> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups) { }

    public void InitializeGroup(Entity<NodeGroupComponent> group, Node sourceNode)
    {
        InitializeGroup((group.Owner, group.Comp, Query.Comp(group)), sourceNode);
    }

    public void RemoveNode(Entity<NodeGroupComponent> group, Node node)
    {
        RemoveNode((group.Owner, group.Comp, Query.Comp(group)), node);
    }

    public void LoadNodes(Entity<NodeGroupComponent> group, List<Node> groupNodes)
    {
        LoadNodes((group.Owner, group.Comp, Query.Comp(group)), groupNodes);
    }

    public void AfterRemake(Entity<NodeGroupComponent> group, IEnumerable<IGrouping<Entity<NodeGroupComponent>?, Node>> newGroups)
    {
        AfterRemake((group.Owner, group.Comp, Query.Comp(group)), newGroups);
    }

    public string? GetDebugData(Entity<NodeGroupComponent> group)
    {
        return GetDebugData((group.Owner, group.Comp, Query.Comp(group)));
    }
}

/// <summary>
/// A variant of <see cref="NodeGroupHandler{T}"/> that automatically registers the handler and the node group.
/// </summary>
/// <typeparam name="T">Type of the handled node group.</typeparam>
public abstract partial class SingleNodeGroupHandler<T> : NodeGroupHandler<T> where T : class, IComponent
{
    protected abstract ProtoId<NodeGroupPrototype> NodeGroupID { get; }

    public override void RegisterHandler()
    {
        NodeGroupSys.NodeGroupTypes[ProtoMan.Index(NodeGroupID).GroupId] = NodeGroupCompType;
        NodeGroupSys.NodeGroupHandlers.Add(NodeGroupCompType, this);
    }
}
