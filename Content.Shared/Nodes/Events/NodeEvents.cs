using Content.Shared.Nodes.Components;

namespace Content.Shared.Nodes.Events;

[ByRefEvent]
public readonly record struct AddedToGraphEvent
(EntityUid NodeId, EntityUid GraphId, (EntityUid Uid, NodeGraphComponent Comp)? OldGraph, GraphNodeComponent Graph, NodeGraphComponent Node);

[ByRefEvent]
public readonly record struct RemovedFromGraphEvent
(EntityUid NodeId, EntityUid GraphId, (EntityUid Uid, NodeGraphComponent Comp)? NextGraph, GraphNodeComponent Graph, NodeGraphComponent Node);
