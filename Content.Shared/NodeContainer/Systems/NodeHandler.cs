using Robust.Shared.Map.Components;

namespace Content.Shared.NodeContainer.Systems;

public interface INodeHandler
{
    /// <summary>
    /// Registers this handler in the <see cref="NodeGroupSystem"/>.
    /// </summary>
    void Register();

    /// <summary>
    ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
    ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set of nodes returned can be asymmetrical
    /// (meaning that it can return other nodes whose <see cref="GetReachableNodes"/> does not return this node).
    /// If this is used, creation of a new node may not correctly merge networks unless both sides
    /// of this asymmetric relation are made to manually update with <see cref="NodeGroupSystem.QueueReflood"/>.
    /// </para>
    /// </remarks>
    IEnumerable<INode> GetReachableNodes(INode node);

    void OnAnchorStateChanged(INode node, bool anchored);

    /// <summary>
    ///     Invoked when the owning <see cref="NodeContainerComponent"/> is initialized.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="owner">The owning entity.</param>
    void InitializeNode(INode node, EntityUid owner);

    /// <summary>
    ///     If this node should be considered for connection by other nodes.
    /// </summary>
    bool Connectable(INode node);

    /// <summary>
    /// Text that the players see when examining this <see cref="INode"/>.
    /// </summary>
    string? GetExamineText(INode node);
}

public abstract partial class NodeHandler<T> : EntitySystem, INodeHandler where T : INode
{
    protected Type NodeType => typeof(T);

    [Dependency] protected SharedMapSystem MapSystem = default!;
    [Dependency] protected NodeGroupSystem NodeGroupSys = default!;
    [Dependency] protected EntityQuery<NodeContainerComponent> NodeQuery = default!;
    [Dependency] protected EntityQuery<MapGridComponent> MapGridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        Register();
    }

    public virtual void Register()
    {
        NodeGroupSys.NodeHandlers.Add(NodeType, this);
    }

    public IEnumerable<Node> GetNodesInTile(Entity<MapGridComponent> grid, Vector2i coords)
    {
        foreach (var entityUid in MapSystem.GetAnchoredEntities(grid, coords))
        {
            if (!NodeQuery.TryGetComponent(entityUid, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                yield return node;
            }
        }
    }

    public IEnumerable<(Direction dir, Node node)> GetCardinalNeighborNodes(
        Entity<MapGridComponent> grid,
        Vector2i coords,
        bool includeSameTile = true)
    {
        foreach (var (dir, entityUid) in GetCardinalNeighborCells(grid, coords, includeSameTile))
        {
            if (!NodeQuery.TryGetComponent(entityUid, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                yield return (dir, node);
            }
        }
    }

    public IEnumerable<(Direction dir, EntityUid entity)> GetCardinalNeighborCells(
        Entity<MapGridComponent> grid,
        Vector2i coords,
        bool includeSameTile = true)
    {
        if (includeSameTile)
        {
            foreach (var uid in MapSystem.GetAnchoredEntities(grid, coords))
            {
                yield return (Direction.Invalid, uid);
            }
        }

        foreach (var uid in MapSystem.GetAnchoredEntities(grid, coords + (0, 1)))
        {
            yield return (Direction.North, uid);
        }

        foreach (var uid in MapSystem.GetAnchoredEntities(grid, coords + (0, -1)))
        {
            yield return (Direction.South, uid);
        }

        foreach (var uid in MapSystem.GetAnchoredEntities(grid, coords + (1, 0)))
        {
            yield return (Direction.East, uid);
        }

        foreach (var uid in MapSystem.GetAnchoredEntities(grid, coords + (-1, 0)))
        {
            yield return (Direction.West, uid);
        }
    }

    /// <summary>
    ///     How this node will attempt to find other reachable <see cref="Node"/>s to group with.
    ///     Returns a set of <see cref="Node"/>s to consider grouping with. Should not return this current <see cref="Node"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set of nodes returned can be asymmetrical
    /// (meaning that it can return other nodes whose <see cref="GetReachableNodes"/> does not return this node).
    /// If this is used, creation of a new node may not correctly merge networks unless both sides
    /// of this asymmetric relation are made to manually update with <see cref="NodeGroupSystem.QueueReflood"/>.
    /// </para>
    /// </remarks>
    protected abstract IEnumerable<Node> GetReachableNodes(T node, Entity<TransformComponent> xform, Entity<MapGridComponent>? grid);

    protected virtual void OnAnchorStateChanged(T node, bool anchored) { }

    /// <summary>
    ///     Invoked when the owning <see cref="NodeContainerComponent"/> is initialized.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="owner">The owning entity.</param>
    protected virtual void Initialize(T node, EntityUid owner)
    {
        node.Owner = owner;
    }

    /// <summary>
    ///     If this node should be considered for connection by other nodes.
    /// </summary>
    protected virtual bool Connectable(T node)
    {
        if (node.Deleting)
            return false;

        if (TerminatingOrDeleted(node.Owner))
            return false;

        if (!node.NeedAnchored)
            return true;

        return Transform(node.Owner).Anchored;
    }

    protected virtual string? GetExamineText(T node)
    {
        return null;
    }

    public IEnumerable<INode> GetReachableNodes(INode node)
    {
        var xform = Transform(node.Owner);
        Entity<TransformComponent> ent = (node.Owner, xform);
        Entity<MapGridComponent>? grid = null;

        if (xform.GridUid != null && MapGridQuery.TryComp(xform.GridUid.Value, out var gridComp))
            grid = (xform.GridUid.Value, gridComp);

        return GetReachableNodes((T) node, ent, grid);
    }

    public void OnAnchorStateChanged(INode node, bool anchored) => OnAnchorStateChanged((T) node, anchored);

    public void InitializeNode(INode node, EntityUid owner) => Initialize((T) node, owner);

    public bool Connectable(INode node) => Connectable((T) node);

    public string? GetExamineText(INode node) => GetExamineText((T) node);
}
