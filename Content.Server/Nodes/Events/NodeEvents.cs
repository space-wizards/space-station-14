using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on the node when a node is added to a graph.</summary>
[ByRefEvent]
public readonly record struct AddedToGraphEvent
(EntityUid NodeId, EntityUid GraphId, (EntityUid Uid, NodeGraphComponent Comp)? OldGraph, GraphNodeComponent Graph, NodeGraphComponent Node);

/// <summary>The event raised on the node when a node is removed from a graph.</summary>
[ByRefEvent]
public readonly record struct RemovedFromGraphEvent
(EntityUid NodeId, EntityUid GraphId, (EntityUid Uid, NodeGraphComponent Comp)? NextGraph, GraphNodeComponent Graph, NodeGraphComponent Node);
