using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on the graph when a node is added to a graph.</summary>
[ByRefEvent]
public readonly record struct NodeAddedEvent
(EntityUid GraphId, EntityUid NodeId, (EntityUid Uid, NodeGraphComponent Comp)? OldGraph, NodeGraphComponent Graph, GraphNodeComponent Node);

/// <summary>The event raised on the graph when a node is removed from a graph.</summary>
[ByRefEvent]
public readonly record struct NodeRemovedEvent
(EntityUid GraphId, EntityUid NodeId, (EntityUid Uid, NodeGraphComponent Comp)? NextGraph, NodeGraphComponent Graph, GraphNodeComponent Node);


/// <summary>The event raised on the graph being merged into before two graphs are merged.</summary>
[ByRefEvent]
public readonly record struct MergingEvent
(EntityUid GraphId, EntityUid MergeId, NodeGraphComponent Graph, NodeGraphComponent Merge);

/// <summary>The event raised on the graph being merged into the other graph before two graphs are merged.</summary>
[ByRefEvent]
public readonly record struct MergingIntoEvent
(EntityUid MergeId, EntityUid GraphId, NodeGraphComponent Merge, NodeGraphComponent Graph);


/// <summary>The event raised on a graph being split before it is split.</summary>
[ByRefEvent]
public readonly record struct SplittingEvent
(EntityUid GraphId, EntityUid SplitId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Graph, NodeGraphComponent Split);

/// <summary>The event raised on a graph being split from the source graph before it is split.</summary>
[ByRefEvent]
public readonly record struct SplittingFromEvent
(EntityUid SplitId, EntityUid GraphId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Split, NodeGraphComponent Graph);

/// <summary>The event raised on a graph being split after it is split.</summary>
[ByRefEvent]
public readonly record struct SplitEvent
(EntityUid GraphId, EntityUid SplitId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Graph, NodeGraphComponent Split);

/// <summary>The event raised on the graph being split after a graphs is split.</summary>
[ByRefEvent]
public readonly record struct SplitFromEvent
(EntityUid SplitId, EntityUid GraphId, List<(EntityUid NodeId, GraphNodeComponent Node)> Nodes, NodeGraphComponent Split, NodeGraphComponent Graph);
