using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.NodeContainer.Nodes;
using Content.Shared.NodeContainer.Nodes.Handlers;

namespace Content.Shared.NodeContainer.Systems;

public sealed partial class NodeContainerSystem : EntitySystem
{
    [Dependency] private NodeGroupSystem _nodeGroupSystem = default!;
    [Dependency] private EntityQuery<NodeContainerComponent> _nodeContainerQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeContainerComponent, ComponentInit>(OnInitEvent);
        SubscribeLocalEvent<NodeContainerComponent, ComponentStartup>(OnStartupEvent);
        SubscribeLocalEvent<NodeContainerComponent, ComponentShutdown>(OnShutdownEvent);
        SubscribeLocalEvent<NodeContainerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<NodeContainerComponent, ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<NodeContainerComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<NodeContainerComponent, ExaminedEvent>(OnExamine);
    }

    [Obsolete("Use an overload that takes in Entity<NodeContainerComponent?> instead")]
    public bool TryGetNode<T>(NodeContainerComponent component, string? identifier, [NotNullWhen(true)] out T? node) where T : Node
    {
        if (identifier == null)
        {
            node = null;
            return false;
        }

        if (component.Nodes.TryGetValue(identifier, out var n) && n is T t)
        {
            node = t;
            return true;
        }

        node = null;
        return false;
    }

    public bool TryGetNode<T>(Entity<NodeContainerComponent?> ent, string identifier, [NotNullWhen(true)] out T? node) where T : Node
    {
        if (_nodeContainerQuery.Resolve(ent, ref ent.Comp, false)
            && ent.Comp.Nodes.TryGetValue(identifier, out var n)
            && n is T t)
        {
            node = t;
            return true;
        }

        node = null;
        return false;
    }

    public bool TryGetNodes<T1, T2>(
        Entity<NodeContainerComponent?> ent,
        string id1,
        string id2,
        [NotNullWhen(true)] out T1? node1,
        [NotNullWhen(true)] out T2? node2)
        where T1 : Node
        where T2 : Node
    {
        if (_nodeContainerQuery.Resolve(ent, ref ent.Comp, false)
            && ent.Comp.Nodes.TryGetValue(id1, out var n1)
            && n1 is T1 t1
            && ent.Comp.Nodes.TryGetValue(id2, out var n2)
            && n2 is T2 t2)
        {
            node1 = t1;
            node2 = t2;
            return true;
        }

        node1 = null;
        node2 = null;
        return false;
    }

    public bool TryGetNodes<T1, T2, T3>(
        Entity<NodeContainerComponent?> ent,
        string id1,
        string id2,
        string id3,
        [NotNullWhen(true)] out T1? node1,
        [NotNullWhen(true)] out T2? node2,
        [NotNullWhen(true)] out T3? node3)
        where T1 : Node
        where T2 : Node
        where T3 : Node
    {
        if (_nodeContainerQuery.Resolve(ent, ref ent.Comp, false)
            && ent.Comp.Nodes.TryGetValue(id1, out var n1)
            && n1 is T1 t1
            && ent.Comp.Nodes.TryGetValue(id2, out var n2)
            && n2 is T2 t2
            && ent.Comp.Nodes.TryGetValue(id3, out var n3)
            && n3 is T3 t3)
        {
            node1 = t1;
            node2 = t2;
            node3 = t3;
            return true;
        }

        node1 = null;
        node2 = null;
        node3 = null;
        return false;
    }

    /// <summary>
    /// Gets the first node of type <see cref="T"/> on the entity.
    /// </summary>
    /// <param name="ent">The entity to get the node grom.</param>
    /// <param name="node">The first node of a target type.</param>
    /// <typeparam name="T">The type of node to look for.</typeparam>
    /// <returns></returns>
    public bool TryGetFirstNode<T>(Entity<NodeContainerComponent?> ent, [NotNullWhen(true)] out T? node) where T : Node
    {
        node = null;
        if (!_nodeContainerQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var n in ent.Comp.Nodes.Values)
        {
            if (n is not T tn)
                continue;

            node = tn;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the first node group of type <see cref="T"/> from the entity's nodes.
    /// </summary>
    /// <param name="ent">The entity to get the node grom.</param>
    /// <param name="node">The first node of a target type.</param>
    /// <typeparam name="T">The type of node to look for.</typeparam>
    /// <returns></returns>
    public bool TryGetFirstNodeGroup<T>(Entity<NodeContainerComponent?> ent, [NotNullWhen(true)] out T? node) where T : BaseNodeGroup
    {
        node = null;
        if (!_nodeContainerQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var n in ent.Comp.Nodes.Values)
        {
            if (n.NodeGroup is not T tn)
                continue;

            node = tn;
            return true;
        }

        return false;
    }

    private void OnInitEvent(Entity<NodeContainerComponent> ent, ref ComponentInit args)
    {
        foreach (var (key, node) in ent.Comp.Nodes)
        {
            node.Name = key;
            var handler = _nodeGroupSystem.GetNodeHandler(node);
            handler.InitializeNode(node, ent.Owner);
        }
    }

    private void OnStartupEvent(Entity<NodeContainerComponent> ent, ref ComponentStartup args)
    {
        foreach (var node in ent.Comp.Nodes.Values)
        {
            _nodeGroupSystem.QueueReflood(node);
        }
    }

    private void OnShutdownEvent(Entity<NodeContainerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var node in ent.Comp.Nodes.Values)
        {
            _nodeGroupSystem.QueueNodeRemove(node);
            node.Deleting = true;
        }
    }

    private void OnAnchorStateChanged(
        EntityUid uid,
        NodeContainerComponent component,
        ref AnchorStateChangedEvent args)
    {
        foreach (var node in component.Nodes.Values)
        {
            if (!node.NeedAnchored)
                continue;

            var handler = _nodeGroupSystem.GetNodeHandler(node);
            handler.OnAnchorStateChanged(node, args.Anchored);

            if (args.Anchored)
                _nodeGroupSystem.QueueReflood(node);
            else
                _nodeGroupSystem.QueueNodeRemove(node);
        }
    }

    private void OnReAnchor(Entity<NodeContainerComponent> ent, ref ReAnchorEvent args)
    {
        foreach (var node in ent.Comp.Nodes.Values)
        {
            _nodeGroupSystem.QueueNodeRemove(node);
            _nodeGroupSystem.QueueReflood(node);
        }
    }

    private void OnMoveEvent(EntityUid uid, NodeContainerComponent container, ref MoveEvent ev)
    {
        if (ev.NewRotation == ev.OldRotation)
        {
            return;
        }

        foreach (var node in container.Nodes.Values)
        {
            if (node is not IRotatableNode rotatableNode)
                continue;

            var handler = (IRotatableNodeHandler) _nodeGroupSystem.GetNodeHandler(node);
            // Don't bother updating nodes that can't even be connected to anything atm.
            if (!handler.Connectable(node))
                continue;

            if (handler.RotateNode(rotatableNode, in ev))
                _nodeGroupSystem.QueueReflood(node);
        }
    }

    private void OnExamine(Entity<NodeContainerComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Examinable || !args.IsInDetailsRange)
            return;

        foreach (var node in ent.Comp.Nodes.Values)
        {
            switch (node.NodeGroupID)
            {
                case NodeGroupID.HVPower:
                    args.PushMarkup(
                        Loc.GetString("node-container-component-on-examine-details-hvpower"));
                    break;
                case NodeGroupID.MVPower:
                    args.PushMarkup(
                        Loc.GetString("node-container-component-on-examine-details-mvpower"));
                    break;
                case NodeGroupID.Apc:
                    args.PushMarkup(
                        Loc.GetString("node-container-component-on-examine-details-apc"));
                    break;
            }
        }
    }
}
