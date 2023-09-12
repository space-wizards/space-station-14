using Content.Server.Nodes.Components;
using Content.Shared.Nodes;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on both involved nodes when an edge is added between two nodes.</summary>
[ByRefEvent]
public readonly record struct EdgeAddedEvent
(EntityUid NodeId, EntityUid EdgeId, EdgeFlags Flags, GraphNodeComponent Node, GraphNodeComponent Edge);

/// <summary>The event raised on both involved nodes when an edge is removed between two nodes.</summary>
[ByRefEvent]
public readonly record struct EdgeRemovedEvent
(EntityUid NodeId, EntityUid EdgeId, EdgeFlags Flags, GraphNodeComponent Node, GraphNodeComponent Edge);

/// <summary>The event raised on both involved nodes when the state of an edge between two nodes is changed (but not added or removed).</summary>
[ByRefEvent]
public readonly record struct EdgeChangedEvent
(EntityUid NodeId, EntityUid EdgeId, EdgeFlags NewFlags, EdgeFlags OldFlags, GraphNodeComponent Node, GraphNodeComponent Edge);


/// <summary>The event raised when recalculating autolinker-derived edges for a node.</summary>
[ByRefEvent]
public record struct UpdateEdgesEvent(EntityUid NodeId, EntityUid HostId, GraphNodeComponent Node)
{
    /// <summary>The uid of the node that is recalculating its edges.</summary>
    public readonly EntityUid NodeId = NodeId;
    /// <summary>The uid of the entity the node is acting as a proxy for, possibly itself.</summary>
    public readonly EntityUid HostId = HostId;
    /// <summary>The state of the node that is recalculating its edges.</summary>
    public readonly GraphNodeComponent Node = Node;

    /// <summary>The set of nodes that the source node should have edges with.</summary>
    public Dictionary<EntityUid, EdgeFlags>? Edges = null;
}

/// <summary>The event raised when checking whether autolinkers want an edge to exist/continue to exist.</summary>
[ByRefEvent]
public record struct CheckEdgeEvent(EntityUid NodeId, EntityUid NodeHostId, EntityUid EdgeId, EntityUid EdgeHostId, GraphNodeComponent Node, GraphNodeComponent Edge, EdgeFlags? OldFlags)
{
    /// <summary>The uid of the node that is checking whether it wants the edge to exist.</summary>
    public readonly EntityUid NodeId = NodeId;
    /// <summary>The uid of the host entity for the source node.</summary>
    public readonly EntityUid NodeHostId = NodeHostId;
    /// <summary>The uid of the node that the source node is checking for an edge with.</summary>
    public readonly EntityUid EdgeId = EdgeId;
    /// <summary>The uid of the host entity for the above.</summary>
    public readonly EntityUid EdgeHostId = EdgeHostId;
    /// <summary>The node that is checking whether the given edge should exist.</summary>
    public readonly GraphNodeComponent Node = Node;
    /// <summary>The node that is at the other end of the edge being checked.</summary>
    public readonly GraphNodeComponent Edge = Edge;
    /// <summary>The set of flags that the edge already had (or null if it didn't exist).</summary>
    public readonly EdgeFlags? OldFlags = OldFlags;

    /// <summary>The additional flags that should be applied to the edge if it is wanted.</summary>
    public EdgeFlags Flags = Components.Edge.DefaultFlags;
    /// <summary>Whether some autolinker wants this edge to exist.</summary>
    public bool Wanted = false;
}
