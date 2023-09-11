using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    /// <summary>
    /// Fetches an empty graph to be filled with nodes.
    /// </summary>
    /// <remarks>
    /// Exists entirely so that graphs may be easily pooled in the future to cut down on entity alloc/deallocs.
    /// </remarks>
    protected (EntityUid GraphId, NodeGraphComponent Graph) SpawnGraph(string? graphProto)
    {
        var graphId = EntityManager.CreateEntityUninitialized(graphProto, GraphRegistry);
        var graph = GraphQuery.GetComponent(graphId);

        graph.GraphProto = graphProto;
        EntityManager.InitializeAndStartEntity(graphId);

        return (graphId, GraphQuery.GetComponent(graphId));
    }

    /// <summary>
    /// Disposes of a graph.
    /// </summary>
    /// <remarks>
    /// Exists entirely so that graphs may be easily pooled in the future to cut down on entity alloc/deallocs.
    /// </remarks>
    protected void DelGraph(EntityUid graphId, NodeGraphComponent _)
    {
        QueueDel(graphId);
    }

    protected void AddNode(EntityUid graphId, EntityUid nodeId, NodeGraphComponent? graph = null, GraphNodeComponent? node = null)
    {
        if (!Resolve(graphId, ref graph) || !Resolve(nodeId, ref node))
            return;

        (EntityUid Uid, NodeGraphComponent Comp)? oldGraph = null;
        if (node.GraphId is { } oldGraphId)
        {
            if (oldGraphId == graphId || !GraphQuery.TryGetComponent(oldGraphId, out var oldGraphComp))
                return;

            oldGraph = (oldGraphId, oldGraphComp);
            RemoveNode(oldGraphId, nodeId, nextGraph: (graphId, graph), graph: oldGraphComp, node: node);
        }

        Dirty(graphId, graph);
        Dirty(nodeId, node);
        node.GraphId = graphId;
        graph.Nodes.Add(nodeId);

        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            QueueMerge(graphId, nodeId, graph);
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            QueueSplit(graphId, nodeId, graph);
        node.DebugColor = graph.DebugColor;

        var graphEv = new NodeAddedEvent(graphId, nodeId, oldGraph, graph, node);
        RaiseLocalEvent(graphId, ref graphEv);
        var nodeEv = new AddedToGraphEvent(nodeId, graphId, oldGraph, node, graph);
        RaiseLocalEvent(nodeId, ref nodeEv);
    }

    protected void RemoveNode(EntityUid graphId, EntityUid nodeId, (EntityUid Uid, NodeGraphComponent Comp)? nextGraph = null, NodeGraphComponent? graph = null, GraphNodeComponent? node = null)
    {
        if (!Resolve(graphId, ref graph) || !Resolve(nodeId, ref node))
            return;

        if (node.GraphId != graphId)
            return;

        Dirty(graphId, graph);
        Dirty(nodeId, node);
        graph.Nodes.Remove(nodeId);
        node.GraphId = null;

        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            CancelMerge(graphId, nodeId, graph);
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            CancelSplit(graphId, nodeId, graph);
        node.DebugColor = null;

        var graphEv = new NodeRemovedEvent(graphId, nodeId, nextGraph, graph, node);
        RaiseLocalEvent(graphId, ref graphEv);
        var nodeEv = new RemovedFromGraphEvent(nodeId, graphId, nextGraph, node, graph);
        RaiseLocalEvent(nodeId, ref nodeEv);

        if (graph.Nodes.Count <= 0)
            DelGraph(graphId, graph);
    }

    protected void QueueSplit(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.SplitNodes.Add(nodeId) && graph.SplitNodes.Count == 1)
            QueuedSplitGraphs.Add(graphId);
    }
    protected void QueueMerge(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.MergeNodes.Add(nodeId) && graph.MergeNodes.Count == 1)
            QueuedMergeGraphs.Add(graphId);
    }
    protected void CancelSplit(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.SplitNodes.Remove(nodeId) && graph.SplitNodes.Count <= 0)
            QueuedSplitGraphs.Remove(graphId);
    }
    protected void CancelMerge(EntityUid graphId, EntityUid nodeId, NodeGraphComponent graph)
    {
        if (graph.MergeNodes.Remove(nodeId) && graph.MergeNodes.Count <= 0)
            QueuedMergeGraphs.Remove(graphId);
    }

    protected void MergeGraphs(EntityUid graphId, EntityUid mergeId, NodeGraphComponent? graph = null, NodeGraphComponent? merge = null)
    {
        if (!Resolve(graphId, ref graph) || !Resolve(mergeId, ref merge))
            return;

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
            AddNode(graphId, nodeId, graph: graph, node: NodeQuery.GetComponent(nodeId));
        }
    }

    protected void SplitGraph(EntityUid graphId, List<(EntityUid NodeId, GraphNodeComponent Node)> nodes, NodeGraphComponent? graph = null)
    {
        if (!Resolve(graphId, ref graph))
            return;

        if (nodes.Count <= 0)
            return;

        var (splitId, split) = SpawnGraph(graph.GraphProto);

        var preGraphEv = new SplittingEvent(graphId, splitId, nodes, graph, split);
        RaiseLocalEvent(graphId, ref preGraphEv);
        var preSplitEv = new SplittingFromEvent(splitId, graphId, nodes, split, graph);
        RaiseLocalEvent(splitId, ref preSplitEv);

        foreach (var (nodeId, node) in nodes)
        {
            AddNode(splitId, nodeId, graph: split, node: node);
        }

        if (graph.Nodes.Count > 0)
        {
            var postGraphEv = new SplitEvent(graphId, splitId, nodes, graph, split);
            RaiseLocalEvent(graphId, ref postGraphEv);
        }
        if (split.Nodes.Count > 0)
        {
            var postSplitEv = new SplitFromEvent(splitId, graphId, nodes, split, graph);
            RaiseLocalEvent(splitId, ref postSplitEv);
        }
        else
            DelGraph(splitId, split);
    }

    protected void FloodSpawnGraph(EntityUid seedId, GraphNodeComponent seed)
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

                var edge = NodeQuery.GetComponent(edgeId);
                if (edge.GraphProto != graphProto)
                    continue;

                if ((edge.Flags & NodeFlags.Init) == NodeFlags.None)
                    UpdateEdges(edgeId, edge);

                if (edge.GraphId is { } edgeGraphId)
                {
                    if (edgeGraphId == graphId || !GraphQuery.TryGetComponent(edgeGraphId, out var edgeGraph))
                        continue;

                    if (graph is null || graph.Nodes.Count < edgeGraph.Nodes.Count)
                        (graphId, graph) = (edgeGraphId, edgeGraph);
                    continue;
                }

                if ((edge.Flags & NodeFlags.Init) != NodeFlags.None)
                    continue;

                edge.Flags |= NodeFlags.Init;
                nodes.Add((edgeId, edge));
            }
        }

        if (graphId is null || graph is null)
            (graphId, graph) = SpawnGraph(graphProto);

        foreach (var (nodeId, node) in nodes)
        {
            AddNode(graphId.Value, nodeId, graph: graph, node: node);
        }

        if (graph.MergeNodes.Count > 0)
            ResolveMerges(graphId.Value, TimeSpan.MinValue, graph);
    }

    protected void ResolveSplits(EntityUid graphId, TimeSpan curTime, NodeGraphComponent graph)
    {
        List<(EntityUid NodeID, GraphNodeComponent Node)>? keeping = null;
        var splitting = new List<(EntityUid NodeID, GraphNodeComponent Node)>();
        while (graph.SplitNodes.FirstOrNull() is { } seedId)
        {
            var seed = NodeQuery.GetComponent(seedId);

            seed.LastUpdate = curTime;
            splitting.Add((seedId, seed));
            for (var i = 0; i < splitting.Count; ++i)
            {
                var (nodeId, node) = splitting[i];
                if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                {
                    ClearSplit(nodeId, node, graph);
                    if (keeping is null && graph.SplitNodes.Count <= 0)
                        return;
                }

                foreach (var (edgeId, edgeFlags) in node.Edges)
                {
                    if ((edgeFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
                        continue;

                    var edge = NodeQuery.GetComponent(edgeId);
                    if (edge.GraphId != node.GraphId)
                        continue;

                    if (edge.LastUpdate is { } && edge.LastUpdate >= curTime)
                        continue;

                    edge.LastUpdate = curTime;
                    splitting.Add((edgeId, edge));
                }
            }

            if (keeping is null)
            {
                (keeping, splitting) = (splitting, new());
                continue;
            }

            if (keeping.Count < splitting.Count)
                (keeping, splitting) = (splitting, keeping);

            SplitGraph(graphId, splitting, graph);
            splitting.Clear();
        }
    }

    protected (EntityUid GraphId, NodeGraphComponent Graph) ResolveMerges(EntityUid graphId, TimeSpan curTime, NodeGraphComponent graph)
    {
        graph.LastUpdate = curTime;
        var graphs = new List<(EntityUid GraphId, NodeGraphComponent Graph)>()
        {
            (graphId, graph),
        };
        for (var i = 0; i < graphs.Count; ++i)
        {
            var (currId, curr) = graphs[i];
            while (curr.MergeNodes.FirstOrNull() is { } nodeId)
            {
                var node = NodeQuery.GetComponent(nodeId);
                ClearMerge(nodeId, node, curr);

                foreach (var (edgeId, edgeFlags) in node.Edges)
                {
                    if ((edgeFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
                        continue;

                    var edge = NodeQuery.GetComponent(edgeId);
                    if (edge.GraphId is not { } mergeId || mergeId == node.GraphId)
                        continue;
                    if (edge.GraphProto != node.GraphProto)
                        continue;

                    if (!GraphQuery.TryGetComponent(mergeId, out var merge))
                        continue;
                    if (merge.LastUpdate is { } && merge.LastUpdate >= curTime)
                        continue;

                    merge.LastUpdate = curTime;
                    graphs.Add((mergeId, merge));
                }
            }

            if (curr.Nodes.Count > graph.Nodes.Count)
                ((graphId, graph), graphs[i]) = ((currId, curr), (graphId, graph));
        }

        for (var i = 1; i < graphs.Count; ++i)
        {
            var (mergeId, merge) = graphs[i];
            MergeGraphs(graphId, mergeId, graph: graph, merge: merge);
        }
        return (graphId, graph);
    }

    protected virtual void OnComponentShutdown(EntityUid uid, NodeGraphComponent comp, ComponentShutdown args)
    {
        if (comp.Nodes.Count <= 0)
            return;

        Log.Error($"Node graph {ToPrettyString(uid)} was shut down while still containing graph nodes. This should never happen.");
        while (comp.Nodes.FirstOrNull() is { } nodeId)
        {
            RemoveNode(uid, nodeId, graph: comp, node: NodeQuery.GetComponent(nodeId));
        }

        DebugTools.Assert(comp.MergeNodes.Count <= 0, "Shut down node graph contained merge nodes after purging nodes.");
        DebugTools.Assert(comp.SplitNodes.Count <= 0, "Show down node graph contained split nodes after purging nodes.");
    }
}
