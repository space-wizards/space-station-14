using Content.Shared.Nodes.Components;

namespace Content.Shared.Nodes.Events;

[ByRefEvent]
public readonly record struct NodeAddedEvent
(EntityUid GraphId, EntityUid NodeId, (EntityUid Uid, NodeGraphComponent Comp)? OldGraph, NodeGraphComponent Graph, GraphNodeComponent Node);

[ByRefEvent]
public readonly record struct NodeRemovedEvent
(EntityUid GraphId, EntityUid NodeId, (EntityUid Uid, NodeGraphComponent Comp)? NextGraph, NodeGraphComponent Graph, GraphNodeComponent Node);

[ByRefEvent]
public readonly record struct MergingEvent
(EntityUid GraphId, EntityUid MergeId, NodeGraphComponent Graph, NodeGraphComponent Merge);

[ByRefEvent]
public readonly record struct MergingIntoEvent
(EntityUid MergeId, EntityUid GraphId, NodeGraphComponent Merge, NodeGraphComponent Graph);

[ByRefEvent]
public readonly record struct SplittingEvent
(EntityUid GraphId, EntityUid SplitId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Graph, NodeGraphComponent Split);

[ByRefEvent]
public readonly record struct SplittingFromEvent
(EntityUid SplitId, EntityUid GraphId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Split, NodeGraphComponent Graph);

[ByRefEvent]
public readonly record struct SplitEvent
(EntityUid GraphId, EntityUid SplitId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Graph, NodeGraphComponent Split);

[ByRefEvent]
public readonly record struct SplitFromEvent
(EntityUid SplitId, EntityUid GraphId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Split, NodeGraphComponent Graph);
