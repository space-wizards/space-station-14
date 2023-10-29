using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on the graph when a node is added to a graph.</summary>
[ByRefEvent]
public readonly record struct NodeAddedEvent(Entity<NodeGraphComponent> Graph, Entity<GraphNodeComponent> Node, Entity<NodeGraphComponent>? OldGraph);

/// <summary>The event raised on the graph when a node is removed from a graph.</summary>
[ByRefEvent]
public readonly record struct NodeRemovedEvent(Entity<NodeGraphComponent> Graph, Entity<GraphNodeComponent> Node, Entity<NodeGraphComponent>? NextGraph);


/// <summary>The event raised on the graph being merged into before two graphs are merged.</summary>
[ByRefEvent]
public readonly record struct MergingEvent(Entity<NodeGraphComponent> Graph, Entity<NodeGraphComponent> Merge);

/// <summary>The event raised on the graph being merged into the other graph before two graphs are merged.</summary>
[ByRefEvent]
public readonly record struct MergingIntoEvent(Entity<NodeGraphComponent> Merge, Entity<NodeGraphComponent> Graph);


/// <summary>The event raised on a graph being split before it is split.</summary>
[ByRefEvent]
public readonly record struct SplittingEvent(Entity<NodeGraphComponent> Graph, Entity<NodeGraphComponent> Split, List<Entity<GraphNodeComponent>> Nodes);

/// <summary>The event raised on a graph being split from the source graph before it is split.</summary>
[ByRefEvent]
public readonly record struct SplittingFromEvent(Entity<NodeGraphComponent> Split, Entity<NodeGraphComponent> Graph, List<Entity<GraphNodeComponent>> Nodes);

/// <summary>The event raised on a graph being split after it is split.</summary>
[ByRefEvent]
public readonly record struct SplitEvent(Entity<NodeGraphComponent> Graph, Entity<NodeGraphComponent> Split, List<Entity<GraphNodeComponent>> Nodes);

/// <summary>The event raised on the graph being split after a graphs is split.</summary>
[ByRefEvent]
public readonly record struct SplitFromEvent(Entity<NodeGraphComponent> Split, Entity<NodeGraphComponent> Graph, List<Entity<GraphNodeComponent>> Nodes);
