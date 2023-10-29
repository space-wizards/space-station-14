using Content.Server.Nodes.Components;
using Content.Shared.Nodes;
using Robust.Shared.Map.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on both involved nodes when an edge is added between two nodes.</summary>
[ByRefEvent]
public readonly record struct EdgeAddedEvent(Entity<GraphNodeComponent> From, Entity<GraphNodeComponent> To, EdgeFlags Flags);

/// <summary>The event raised on both involved nodes when an edge is removed between two nodes.</summary>
[ByRefEvent]
public readonly record struct EdgeRemovedEvent(Entity<GraphNodeComponent> From, Entity<GraphNodeComponent> To, EdgeFlags Flags);

/// <summary>The event raised on both involved nodes when the state of an edge between two nodes is changed (but not added or removed).</summary>
[ByRefEvent]
public readonly record struct EdgeChangedEvent(Entity<GraphNodeComponent> From, Entity<GraphNodeComponent> To, EdgeFlags NewFlags, EdgeFlags OldFlags);


/// <summary>The event raised when recalculating autolinker-derived edges for a node.</summary>
[ByRefEvent]
public record struct UpdateEdgesEvent(Entity<GraphNodeComponent> Node, Entity<TransformComponent?> Host, Entity<MapGridComponent>? Grid)
{
    /// <summary>The node that is recalculating its edges.</summary>
    public readonly Entity<GraphNodeComponent> Node = Node;
    /// <summary>The entity that the node is attached to/part of.</summary>
    public readonly Entity<TransformComponent?> Host = Host;
    /// <summary>The grid that the entity is located on.</summary>
    public readonly Entity<MapGridComponent>? Grid = Grid;
    /// <summary>The set of edges that the handlers think the node should have.</summary>
    public Dictionary<EntityUid, EdgeFlags>? Edges = null;
}

/// <summary>The event raised when checking whether autolinkers want an edge to exist/continue to exist.</summary>
[ByRefEvent]
public record struct CheckEdgeEvent(
    Entity<GraphNodeComponent> From, Entity<GraphNodeComponent> To,
    Entity<TransformComponent?> FromHost, Entity<TransformComponent?> ToHost,
    Entity<MapGridComponent>? FromGrid, Entity<MapGridComponent>? ToGrid,
    EdgeFlags? OldFlags
)
{
    /// <summary>The node that the checked edge originates at.</summary>
    public readonly Entity<GraphNodeComponent> From = From;
    /// <summary>The node that the checked edge terminates at.</summary>
    public readonly Entity<GraphNodeComponent> To = To;
    /// <summary>The entity that the origin node is attached to/part of.</summary>
    public readonly Entity<TransformComponent?> FromHost = FromHost;
    /// <summary>The entity that the destination node is attached to/part of.</summary>
    public readonly Entity<TransformComponent?> ToHost = ToHost;
    /// <summary>The grid that the origin entity is located on.</summary>
    public readonly Entity<MapGridComponent>? FromGrid = FromGrid;
    /// <summary>The grid that the destination entity is located on.</summary>
    public readonly Entity<MapGridComponent>? ToGrid = ToGrid;
    /// <summary>The flags the edge currently has.</summary>
    public readonly EdgeFlags? OldFlags = OldFlags;
    /// <summary>The flags the handlers say the edge should have.</summary>
    public EdgeFlags? Flags = null;
    /// <summary>Whether the handlers want the edge to exist.</summary>
    public bool Wanted = false;
}
