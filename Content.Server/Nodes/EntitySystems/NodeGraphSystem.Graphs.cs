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
    private (EntityUid GraphId, NodeGraphComponent Graph) SpawnGraph(string graphProto)
    {
        var graphId = EntityManager.CreateEntityUninitialized(graphProto);

        var graph = _graphQuery.GetComponent(graphId);
        graph.GraphProto = graphProto;

        EntityManager.InitializeEntity(graphId);
        // Startup occurrs after all nodes have been loaded.
        return (graphId, graph);
    }

    /// <summary>
    /// Disposes of a graph.
    /// </summary>
    /// <remarks>
    /// Exists entirely so that graphs may be easily pooled in the future to cut down on entity alloc/deallocs.
    /// </remarks>
    private void DelGraph(EntityUid graphId, NodeGraphComponent _)
    {
        QueueDel(graphId);
    }

    /// <summary>
    /// Floodfills connected, uninitialized nodes with a new graph.
    /// </summary>
    private void FloodSpawnGraph(EntityUid seedId, GraphNodeComponent seed)
    {
        var graphProto = seed.GraphProto;
        EntityUid? graphId = null;
        NodeGraphComponent? graph = null;

        seed.Flags |= NodeFlags.Init;
        var nodes = new List<(EntityUid NodeId, GraphNodeComponent Nodes)>()
        {
            (seedId, seed),
        };
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
                    UpdateEdges(edgeId, edge);

                // We have encountered a mergeable edge leading to some compatible graph:
                if (edge.GraphId is { } edgeGraphId)
                {
                    if (edgeGraphId == graphId)
                        continue; // Tis the graph we are using to floodfill, don't bother propagating into it b/c we'd just add it to its own graph.

                    // Floodfill using largest adjacent extant graph if possible to minimize node handoffs. 
                    var edgeGraph = _graphQuery.GetComponent(edgeGraphId);
                    if (graph is null || graph.Nodes.Count < edgeGraph.Nodes.Count)
                        (graphId, graph) = (edgeGraphId, edgeGraph);
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
        if (graphId is null || graph is null)
            (graphId, graph) = SpawnGraph(graphProto);

        // Add all connected, compatible nodes into the new graph (at least 1 due to seed).
        foreach (var (nodeId, node) in nodes)
        {
            AddNode(graphId.Value, nodeId, graph: graph, node: node);
        }

        EntityManager.StartEntity(graphId.Value);
    }


    /// <summary>
    /// Adds a node to a graph.
    /// </summary>
    private void AddNode(EntityUid graphId, EntityUid nodeId, NodeGraphComponent? graph = null, GraphNodeComponent? node = null)
    {
        if (!_graphQuery.Resolve(graphId, ref graph) || !_nodeQuery.Resolve(nodeId, ref node))
            return;

        DebugTools.Assert(graph.GraphProto == node.GraphProto, $"Attempted to add {ToPrettyString(nodeId)} (wants {node.GraphProto}) to incompatible graph {ToPrettyString(graphId)} (is {graph.GraphProto}).");

        (EntityUid Uid, NodeGraphComponent Comp)? oldGraph = null;
        if (node.GraphId is { } oldGraphId)
        {
            if (oldGraphId == graphId)
                return;

            oldGraph = (oldGraphId, _graphQuery.GetComponent(oldGraphId));
            RemoveNode(oldGraphId, nodeId, nextGraph: (graphId, graph), graph: oldGraph.Value.Comp, node: node);
        }

        node.GraphId = graphId;
        graph.Nodes.Add(nodeId);
        Dirty(graphId, graph);
        Dirty(nodeId, node);

        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            QueueMerge(graphId, nodeId, graph);
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            QueueSplit(graphId, nodeId, graph);

        var graphEv = new NodeAddedEvent(graphId, nodeId, oldGraph, graph, node);
        RaiseLocalEvent(graphId, ref graphEv);
        var nodeEv = new AddedToGraphEvent(nodeId, graphId, oldGraph, node, graph);
        RaiseLocalEvent(nodeId, ref nodeEv);
    }

    /// <summary>
    /// Removes a node from a graph.
    /// </summary>
    private void RemoveNode(EntityUid graphId, EntityUid nodeId, (EntityUid Uid, NodeGraphComponent Comp)? nextGraph = null, NodeGraphComponent? graph = null, GraphNodeComponent? node = null)
    {
        if (!_graphQuery.Resolve(graphId, ref graph) || !_nodeQuery.Resolve(nodeId, ref node))
            return;

        if (node.GraphId != graphId)
            return;

        graph.Nodes.Remove(nodeId);
        node.GraphId = null;
        Dirty(graphId, graph);
        Dirty(nodeId, node);

        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            CancelMerge(graphId, nodeId, graph);
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            CancelSplit(graphId, nodeId, graph);

        var graphEv = new NodeRemovedEvent(graphId, nodeId, nextGraph, graph, node);
        RaiseLocalEvent(graphId, ref graphEv);
        var nodeEv = new RemovedFromGraphEvent(nodeId, graphId, nextGraph, node, graph);
        RaiseLocalEvent(nodeId, ref nodeEv);

        if (graph.Nodes.Count <= 0)
            DelGraph(graphId, graph);
    }


    /// <summary>
    /// Queues a graph to be checked for splits at a given node.
    /// </summary>
    private void QueueSplit(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.SplitNodes.Add(nodeId) && graph.SplitNodes.Count == 1)
            _queuedSplitGraphs.Add(graphId);
    }

    /// <summary>
    /// Queues a graph to be checked for merging at a given node.
    /// </summary>
    private void QueueMerge(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.MergeNodes.Add(nodeId) && graph.MergeNodes.Count == 1)
            _queuedMergeGraphs.Add(graphId);
    }

    /// <summary>
    /// Cancels a potential split in a graph at a given node.
    /// </summary>
    private void CancelSplit(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.SplitNodes.Remove(nodeId) && graph.SplitNodes.Count <= 0)
            _queuedSplitGraphs.Remove(graphId);
    }

    /// <summary>
    /// Cancels a potential merge between graphs at a given node.
    /// </summary>
    private void CancelMerge(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.MergeNodes.Remove(nodeId) && graph.MergeNodes.Count <= 0)
            _queuedMergeGraphs.Remove(graphId);
    }


    /// <summary>
    /// Merges the smaller of two graphs into the larger.
    /// </summary>
    private void MergeGraphs(ref EntityUid graphId, ref EntityUid mergeId, ref NodeGraphComponent graph, ref NodeGraphComponent merge)
    {
        if (graphId == mergeId)
            return;

        if (graph.Nodes.Count < merge.Nodes.Count)
            (graphId, mergeId, graph, merge) = (mergeId, graphId, merge, graph);

        var graphEv = new MergingEvent(graphId, mergeId, graph, merge);
        RaiseLocalEvent(graphId, ref graphEv);
        var mergeEv = new MergingIntoEvent(mergeId, graphId, merge, graph);
        RaiseLocalEvent(mergeId, ref mergeEv);

        while (merge.Nodes.FirstOrNull() is { } nodeId)
        {
            AddNode(graphId, nodeId, graph: graph, node: _nodeQuery.GetComponent(nodeId));
        }
    }

    /// <summary>
    /// Splits a set of nodes out of a graph and into a new graph.
    /// </summary>
    /// <returns>The split graph or null if no nodes were successfully split from the source graph.</returns>
    private (EntityUid Uid, NodeGraphComponent Comp)? SplitGraph(EntityUid graphId, List<(EntityUid NodeId, GraphNodeComponent Node)> nodes, NodeGraphComponent graph)
    {
        if (nodes.Count <= 0)
            return null;

        var (splitId, split) = SpawnGraph(graph.GraphProto);

        var preGraphEv = new SplittingEvent(graphId, splitId, nodes, graph, split);
        RaiseLocalEvent(graphId, ref preGraphEv);
        var preSplitEv = new SplittingFromEvent(splitId, graphId, nodes, split, graph);
        RaiseLocalEvent(splitId, ref preSplitEv);

        foreach (var (nodeId, node) in nodes)
        {
            AddNode(splitId, nodeId, graph: split, node: node);
        }

        if (split.Nodes.Count <= 0)
        {
            DelGraph(splitId, split);
            return null;
        }

        // If all of the nodes were moved out of the old graph it's being deleted.
        var postGraphEv = new SplitEvent(graphId, splitId, nodes, graph, split);
        RaiseLocalEvent(graphId, ref postGraphEv);

        var postSplitEv = new SplitFromEvent(splitId, graphId, nodes, split, graph);
        RaiseLocalEvent(splitId, ref postSplitEv);
        return (splitId, split);
    }


    /// <summary>
    /// Merges connected, compatible graphs into one large graph.
    /// </summary>
    private (EntityUid GraphId, NodeGraphComponent Graph) ResolveMerges(EntityUid graphId, UpdateIter iter, NodeGraphComponent graph)
    {
        graph.LastUpdate = iter;
        var graphs = new List<(EntityUid GraphId, NodeGraphComponent Graph)>()
        {
            (graphId, graph),
        };
        for (var i = 0; i < graphs.Count; ++i)
        {
            var (currId, curr) = graphs[i];
            while (curr.MergeNodes.FirstOrNull() is { } nodeId)
            {
                var node = _nodeQuery.GetComponent(nodeId);
                ClearMerge(nodeId, node, curr);

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
            if (curr.Nodes.Count > graph.Nodes.Count)
                ((graphId, graph), graphs[i]) = ((currId, curr), (graphId, graph));
        }

        for (var i = 1; i < graphs.Count; ++i)
        {
            var (mergeId, merge) = graphs[i];
            if (mergeId == graphId)
                continue; // Don't merge the graph we are merging everything into into itself.

            MergeGraphs(ref graphId, ref mergeId, graph: ref graph, merge: ref merge);
        }

        return (graphId, graph);
    }

    /// <summary>
    /// Split a potentially unconnected graph into one or more connected subgraphs.
    /// </summary>
    /// <remarks>
    /// On the server this has the potential to be parallelized b/c it never crosses graph boundaries.
    /// Would require converting it to an enumerator for connected sets of nodes and transferring the results to the main thread for spawning.
    /// </remarks>
    private void ResolveSplits(EntityUid graphId, UpdateIter iter, NodeGraphComponent graph)
    {
        List<(EntityUid NodeID, GraphNodeComponent Node)>? keeping = null;
        var splitting = new List<(EntityUid NodeID, GraphNodeComponent Node)>();
        while (graph.SplitNodes.FirstOrNull() is { } seedId)
        {
            var seed = _nodeQuery.GetComponent(seedId);

            seed.LastUpdate = iter;
            splitting.Add((seedId, seed));
            for (var i = 0; i < splitting.Count; ++i)
            {
                var (nodeId, node) = splitting[i];

                if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                {
                    ClearSplit(nodeId, node, graph);
                    // If we have cleared all of the split nodes during the first iteration we don't need to bother BFSing the rest of the nodes because we know it's all one big group.
                    if (keeping is null && graph.SplitNodes.Count <= 0)
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
            SplitGraph(graphId, splitting, graph);

            // TODO??? Would mean that we would need to build a new list for every group though. Not sure what the impact of all the extra heap allocs/deallocs would be compared to the parallelism.
            splitting.Clear();
        }
    }


    #region Event Handlers

    /// <summary>
    /// Ensures that graph states only get sent if debugging info is enabled.
    /// </summary>
    private void OnComponentInit(EntityUid uid, NodeGraphComponent comp, ComponentInit args)
    {
        DebugTools.Assert(comp.GraphProto != default, $"Node graph {uid} spawned without having its type set.");

        comp.NetSyncEnabled = _sendingVisState;

        _graphsByProto.GetOrNew(comp.GraphProto).Add(uid);
    }

    /// <summary>
    /// If a node graphs gets ungraphed with nodes still in it we should complain about it b/c that will result in nodes with null node graphs.
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, NodeGraphComponent comp, ComponentShutdown args)
    {
        if (_graphsByProto.TryGetValue(comp.GraphProto, out var set))
            set.Remove(uid);

        if (comp.Nodes.Count <= 0)
            return;

        Log.Error($"Node graph {ToPrettyString(uid)} was shut down while still containing graph nodes; this should never happen.");

        // Shuffle all of the nodes into the replacement...
        var (subId, sub) = SpawnGraph(comp.GraphProto);
        while (comp.Nodes.FirstOrNull() is { } nodeId)
        {
            var node = _nodeQuery.GetComponent(nodeId);

            AddNode(subId, nodeId, graph: sub, node: node);
            RemoveNode(uid, nodeId, graph: comp, node: node); // To be sure...
        }

        Log.Debug($"Rebuilt graph for nodes of deleted graph {ToPrettyString(uid)}; the new graph is {ToPrettyString(subId)}.");

        DebugTools.Assert(comp.MergeNodes.Count <= 0, "Shut down node graph contained merge nodes after purging nodes.");
        DebugTools.Assert(comp.SplitNodes.Count <= 0, "Show down node graph contained split nodes after purging nodes.");
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
