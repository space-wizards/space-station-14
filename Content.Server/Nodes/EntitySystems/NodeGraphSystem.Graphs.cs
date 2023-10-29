using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;
using Robust.Shared.Utility;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Fetches an empty graph to be filled with nodes.
    /// </summary>
    /// <remarks>
    /// Exists entirely so that graphs may be easily pooled in the future to cut down on entity alloc/deallocs.
    /// </remarks>
    private Entity<NodeGraphComponent> SpawnGraph(string graphProto)
    {
        var graphId = EntityManager.CreateEntityUninitialized(graphProto);

        var graph = _graphQuery.GetComponent(graphId);
        graph.GraphProto = graphProto;

        EntityManager.InitializeEntity(graphId);
        EntityManager.StartEntity(graphId);
        // Startup occurrs after all nodes have been loaded.
        return (graphId, graph);
    }

    /// <summary>
    /// Disposes of a graph.
    /// </summary>
    /// <remarks>
    /// Exists entirely so that graphs may be easily pooled in the future to cut down on entity alloc/deallocs.
    /// </remarks>
    private void DelGraph(Entity<NodeGraphComponent> graph)
    {
        QueueDel(graph);
    }

    /// <summary>
    /// Floodfills connected, uninitialized nodes with a new graph.
    /// </summary>
    private void FloodSpawnGraph(Entity<GraphNodeComponent> seed)
    {
        var graphProto = seed.Comp.GraphProto;
        Entity<NodeGraphComponent>? graph = null;

        seed.Comp.Flags |= NodeFlags.Init;
        var nodes = new List<Entity<GraphNodeComponent>>() { seed, };
        for (var i = 0; i < nodes.Count; ++i)
        {
            var (_, node) = nodes[i];

            foreach (var (edgeId, edgeFlags) in node.Edges)
            {
                if ((edgeFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
                    continue;

                var edge = _nodeQuery.GetComponent(edgeId);
                // Edge node is incompatible. We can't merge with it.
                if (edge.GraphProto != graphProto)
                    continue;

                // Update node edges as necessary so the floodfill actually propagates past the initial node:
                if ((edge.Flags & NodeFlags.Init) == NodeFlags.None)
                    UpdateEdges((edgeId, edge));

                // We have encountered a mergeable edge leading to some compatible graph:
                if (edge.GraphId is { } edgeGraphId)
                {
                    if (edgeGraphId == graph?.Owner)
                        continue; // Tis the graph we are using to floodfill, don't bother propagating into it b/c we'd just add it to its own graph.

                    // Floodfill using largest adjacent extant graph if possible to minimize node handoffs. 
                    var edgeGraph = _graphQuery.GetComponent(edgeGraphId);
                    if (graph is null || graph.Value.Comp.Nodes.Count < edgeGraph.Nodes.Count)
                        graph = (edgeGraphId, edgeGraph);
                    continue;
                }

                // Edge node has already been propagated to so don't for(;;) the BFS.
                if ((edge.Flags & NodeFlags.Init) != NodeFlags.None)
                    continue;

                edge.Flags |= NodeFlags.Init;
                nodes.Add((edgeId, edge));
            }
        }

        // If we didn't find an extant graph to extend make a new one.
        graph ??= SpawnGraph(graphProto);

        // Add all connected, compatible nodes into the new graph (at least 1 due to seed).
        foreach (var node in nodes)
        {
            AddNode(graph.Value, node);
        }

        EntityManager.StartEntity(graph.Value);
    }


    /// <summary>
    /// Adds a node to a graph.
    /// </summary>
    private void AddNode(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node)
    {
        DebugTools.Assert(graph.Comp.GraphProto == node.Comp.GraphProto, $"Attempted to add {ToPrettyString(node)} (wants {node.Comp.GraphProto}) to incompatible graph {ToPrettyString(graph)} (is {graph.Comp.GraphProto}).");

        Entity<NodeGraphComponent>? oldGraph = null;
        if (node.Comp.GraphId is { } oldGraphId)
        {
            if (oldGraphId == graph.Owner)
                return;

            oldGraph = (oldGraphId, _graphQuery.GetComponent(oldGraphId));
            RemoveNode(oldGraph.Value, node, nextGraph: graph);
        }

        node.Comp.GraphId = graph.Owner;
        graph.Comp.Nodes.Add(node);
        Dirty(graph);
        Dirty(node);

        if ((node.Comp.Flags & NodeFlags.Merge) != NodeFlags.None)
            QueueMerge(graph, node);
        if ((node.Comp.Flags & NodeFlags.Split) != NodeFlags.None)
            QueueSplit(graph, node);

        var graphEv = new NodeAddedEvent(graph, node, oldGraph);
        RaiseLocalEvent(graph, ref graphEv);
        var nodeEv = new AddedToGraphEvent(node, graph, oldGraph);
        RaiseLocalEvent(node, ref nodeEv);
    }

    /// <summary>
    /// Removes a node from a graph.
    /// </summary>
    private void RemoveNode(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node, Entity<NodeGraphComponent>? nextGraph = null)
    {
        DebugTools.Assert(node.Comp.GraphId == graph, $"Attempted to remove node {ToPrettyString(node)} from graph {ToPrettyString(graph)} while it was a member of {ToPrettyString(node.Comp.GraphId)}!");

        graph.Comp.Nodes.Remove(node);
        node.Comp.GraphId = null;
        Dirty(graph);
        Dirty(node);

        if ((node.Comp.Flags & NodeFlags.Merge) != NodeFlags.None)
            CancelMerge(graph, node);
        if ((node.Comp.Flags & NodeFlags.Split) != NodeFlags.None)
            CancelSplit(graph, node);

        var graphEv = new NodeRemovedEvent(graph, node, nextGraph);
        RaiseLocalEvent(graph, ref graphEv);
        var nodeEv = new RemovedFromGraphEvent(node, graph, nextGraph);
        RaiseLocalEvent(node, ref nodeEv);

        if (graph.Comp.Nodes.Count <= 0)
            DelGraph(graph);
    }


    /// <summary>
    /// Queues a graph to be checked for splits at a given node.
    /// </summary>
    private void QueueSplit(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node)
    {
        if (graph.Comp.SplitNodes.Add(node) && graph.Comp.SplitNodes.Count == 1)
            _queuedSplitGraphs.Add(graph);
    }

    /// <summary>
    /// Queues a graph to be checked for merging at a given node.
    /// </summary>
    private void QueueMerge(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node)
    {
        if (graph.Comp.MergeNodes.Add(node) && graph.Comp.MergeNodes.Count == 1)
            _queuedMergeGraphs.Add(graph);
    }

    /// <summary>
    /// Cancels a potential split in a graph at a given node.
    /// </summary>
    private void CancelSplit(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node)
    {
        if (graph.Comp.SplitNodes.Remove(node) && graph.Comp.SplitNodes.Count <= 0)
            _queuedSplitGraphs.Remove(graph);
    }

    /// <summary>
    /// Cancels a potential merge between graphs at a given node.
    /// </summary>
    private void CancelMerge(Entity<NodeGraphComponent> graph, Entity<GraphNodeComponent> node)
    {
        if (graph.Comp.MergeNodes.Remove(node) && graph.Comp.MergeNodes.Count <= 0)
            _queuedMergeGraphs.Remove(graph);
    }


    /// <summary>
    /// Merges the smaller of two graphs into the larger.
    /// </summary>
    private void MergeGraphs(ref Entity<NodeGraphComponent> graph, ref Entity<NodeGraphComponent> merge)
    {
        if (graph == merge)
            return;

        if (graph.Comp.Nodes.Count < merge.Comp.Nodes.Count)
            (graph, merge) = (merge, graph);

        var graphEv = new MergingEvent(graph, merge);
        RaiseLocalEvent(graph, ref graphEv);
        var mergeEv = new MergingIntoEvent(merge, graph);
        RaiseLocalEvent(merge, ref mergeEv);

        while (merge.Comp.Nodes.FirstOrNull() is { } nodeId)
        {
            AddNode(graph, (nodeId, _nodeQuery.GetComponent(nodeId)));
        }
    }

    /// <summary>
    /// Splits a set of nodes out of a graph and into a new graph.
    /// </summary>
    /// <returns>The split graph or null if no nodes were successfully split from the source graph.</returns>
    private Entity<NodeGraphComponent>? SplitGraph(Entity<NodeGraphComponent> graph, List<Entity<GraphNodeComponent>> nodes)
    {
        if (nodes.Count <= 0)
            return null;

        var split = SpawnGraph(graph.Comp.GraphProto);

        var preGraphEv = new SplittingEvent(graph, split, nodes);
        RaiseLocalEvent(graph, ref preGraphEv);
        var preSplitEv = new SplittingFromEvent(split, graph, nodes);
        RaiseLocalEvent(split, ref preSplitEv);

        foreach (var node in nodes)
        {
            AddNode(split, node);
        }

        if (split.Comp.Nodes.Count <= 0)
        {
            DelGraph(split);
            return null;
        }

        // If all of the nodes were moved out of the old graph it's being deleted.
        var postGraphEv = new SplitEvent(graph, split, nodes);
        RaiseLocalEvent(graph, ref postGraphEv);

        var postSplitEv = new SplitFromEvent(split, graph, nodes);
        RaiseLocalEvent(split, ref postSplitEv);
        return split;
    }


    /// <summary>
    /// Merges connected, compatible graphs into one large graph.
    /// </summary>
    private Entity<NodeGraphComponent> ResolveMerges(Entity<NodeGraphComponent> graph, UpdateIter iter)
    {
        graph.Comp.LastUpdate = iter;
        var graphs = new List<Entity<NodeGraphComponent>>() { graph, };
        for (var i = 0; i < graphs.Count; ++i)
        {
            var (currId, curr) = graphs[i];
            while (curr.MergeNodes.FirstOrNull() is { } nodeId)
            {
                var node = _nodeQuery.GetComponent(nodeId);
                ClearMerge((nodeId, node), curr);

                foreach (var (edgeId, edgeFlags) in node.Edges)
                {
                    // No
                    if ((edgeFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
                        continue;

                    var edge = _nodeQuery.GetComponent(edgeId);
                    // Can't merge null graphs, this will be handled on their mapinit (also ignore internal edges).
                    if (edge.GraphId is not { } mergeId || mergeId == node.GraphId)
                        continue;
                    // Graphs on either side of the edge are incompatible.
                    if (edge.GraphProto != node.GraphProto)
                        continue;

                    var merge = _graphQuery.GetComponent(mergeId);
                    // The edge graph has already been propagated to during this iteration. Don't for(;;) the BFS.
                    if (merge.LastUpdate is { } && merge.LastUpdate >= iter)
                        continue;

                    merge.LastUpdate = iter;
                    graphs.Add((mergeId, merge));
                }
            }

            // Largest graph is used as the merge target to minimize node handoffs.
            if (curr.Nodes.Count > graph.Comp.Nodes.Count)
                (graph, graphs[i]) = ((currId, curr), graph);
        }

        for (var i = 1; i < graphs.Count; ++i)
        {
            var merge = graphs[i];
            if (merge.Owner == graph.Owner)
                continue; // Don't merge the graph we are merging everything into into itself.

            MergeGraphs(ref graph, ref merge);
        }

        return graph;
    }

    /// <summary>
    /// Split a potentially unconnected graph into one or more connected subgraphs.
    /// </summary>
    /// <remarks>
    /// On the server this has the potential to be parallelized b/c it never crosses graph boundaries.
    /// Would require converting it to an enumerator for connected sets of nodes and transferring the results to the main thread for spawning.
    /// </remarks>
    private void ResolveSplits(Entity<NodeGraphComponent> graph, UpdateIter iter)
    {
        List<Entity<GraphNodeComponent>>? keeping = null;
        var splitting = new List<Entity<GraphNodeComponent>>();
        while (graph.Comp.SplitNodes.FirstOrNull() is { } seedId)
        {
            var seed = _nodeQuery.GetComponent(seedId);

            seed.LastUpdate = iter;
            splitting.Add((seedId, seed));
            for (var i = 0; i < splitting.Count; ++i)
            {
                var (nodeId, node) = splitting[i];

                if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                {
                    ClearSplit((nodeId, node), graph);
                    // If we have cleared all of the split nodes during the first iteration we don't need to bother BFSing the rest of the nodes because we know it's all one big group.
                    if (keeping is null && graph.Comp.SplitNodes.Count <= 0)
                        return;
                }

                foreach (var (edgeId, edgeFlags) in node.Edges)
                {
                    // No merging means that these are splits in the graph.
                    if ((edgeFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
                        continue;

                    var edge = _nodeQuery.GetComponent(edgeId);
                    // Don't propagate into other graphs. We are splitting _this_ graph and this makes parallelism possible.
                    if (edge.GraphId != node.GraphId)
                        continue;

                    // We already propagated to this node this iteration. Don't for(;;) the BFS.
                    if (edge.LastUpdate is { } && edge.LastUpdate >= iter)
                        continue;

                    edge.LastUpdate = iter;
                    splitting.Add((edgeId, edge));
                }
            }

            // Skip the first iteration because we know we are keeping at least one group.
            if (keeping is null)
            {
                (keeping, splitting) = (splitting, new());
                continue;
            }

            // Ensure that we keep the largest group to minimize node handoffs.
            if (keeping.Count < splitting.Count)
                (keeping, splitting) = (splitting, keeping);

            // TODO??? Could be parallelized if this was extracted out to the main thread. (Convert to IEnumerable<...> and aggregate into ConcurrentBox?)
            SplitGraph(graph, splitting);

            // TODO??? Would mean that we would need to build a new list for every group though. Not sure what the impact of all the extra heap allocs/deallocs would be compared to the parallelism.
            splitting.Clear();
        }
    }


    #region Event Handlers

    /// <summary>
    /// If a node graph gets ungraphed with nodes still in it we should complain about it b/c that will result in nodes with null node graphs.
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, NodeGraphComponent comp, ComponentShutdown args)
    {
        DebugTools.Assert(comp.Nodes.Count <= 0, $"Attempted to shut down graph {ToPrettyString(uid)} while it still contained nodes. This should never happen.");
    }

    /// <remarks>
    /// Since graphs should never be naturally deleted except by being emptied of nodes this should always be a noop unless badmins manually delete one.
    /// In that case we don't want there to be any nodes left with a null graph so yeet all the nodes too.
    /// </remarks>
    private void OnEntityTerminating(EntityUid uid, NodeGraphComponent comp, ref EntityTerminatingEvent args)
    {
        if (comp.Nodes.Count <= 0)
            return;

        Log.Info($"Node graph {ToPrettyString(uid)} deleted while still containing nodes; we are yeeting the __entire__ graph.");
        while (comp.Nodes.FirstOrNull() is { } nodeId)
        {
            Del(GetNodeHost(nodeId));
        }
    }

    #endregion Event Handlers
}
