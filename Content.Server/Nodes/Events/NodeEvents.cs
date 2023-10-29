using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event raised on the node when a node is added to a graph.</summary>
[ByRefEvent]
public readonly record struct AddedToGraphEvent(Entity<GraphNodeComponent> Node, Entity<NodeGraphComponent> Graph, Entity<NodeGraphComponent>? OldGraph);

/// <summary>The event raised on the node when a node is removed from a graph.</summary>
[ByRefEvent]
public readonly record struct RemovedFromGraphEvent(Entity<GraphNodeComponent> Node, Entity<NodeGraphComponent> Graph, Entity<NodeGraphComponent>? NextGraph);
