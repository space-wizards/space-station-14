using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;
using Robust.Shared.Utility;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Marks a graph node as the location of a potential split in the graph.
    /// </summary>
    public void MarkSplit(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Split;

        if (node.GraphId is { } graphId && _graphQuery.Resolve(graphId, ref graph, logMissing: false))
            QueueSplit(graphId, nodeId, graph);
    }

    /// <summary>
    /// Marks a graph node as the location of a potential merge between two graphs.
    /// </summary>
    public void MarkMerge(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Merge;

        if (node.GraphId is { } graphId && _graphQuery.Resolve(graphId, ref graph, logMissing: false))
            QueueMerge(graphId, nodeId, graph);
    }

    /// <summary>
    /// Clears a graph node as the location of a potential split in the graph.
    /// </summary>
    public void ClearSplit(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Split) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Split;

        if (node.GraphId is { } graphId && _graphQuery.Resolve(graphId, ref graph, logMissing: false))
            CancelSplit(graphId, nodeId, graph);
    }

    /// <summary>
    /// Clears a graph node as the location of a potential merge between two or more graphs.
    /// </summary>
    public void ClearMerge(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Merge) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Merge;

        if (node.GraphId is { } graphId && _graphQuery.Resolve(graphId, ref graph, logMissing: false))
            CancelMerge(graphId, nodeId, graph);
    }


    /// <summary>
    /// Syncs up the cached number of mergeable edges the node has with its initial edges.
    /// </summary>
    private void OnComponentInit(EntityUid uid, GraphNodeComponent comp, ComponentInit args)
    {
        comp.NumMergeableEdges = 0;
        foreach (var (_, edgeFlags) in comp.Edges)
        {
            if ((edgeFlags & EdgeFlags.NoMerge) == EdgeFlags.None)
                ++comp.NumMergeableEdges;
        }
    }

    /// <summary>
    /// Generates the initial edges and graph for a node.
    /// </summary>
    private void OnMapInit(EntityUid uid, GraphNodeComponent comp, MapInitEvent args)
    {
        if ((comp.Flags & NodeFlags.Init) == NodeFlags.None)
        {
            comp.Flags |= NodeFlags.Init;
            UpdateEdges(uid, comp);
        }

        if (comp.GraphId is not { })
            FloodSpawnGraph(uid, comp);
    }

    /// <summary>
    /// Removes any nodes that are becoming noden'ts from their graphs.
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, GraphNodeComponent comp, ComponentShutdown args)
    {
        // Nodes aren't networked unless debugging info is enabled.
        comp.NetSyncEnabled = _sendingVisState;

        if ((comp.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(uid, comp);

        while (comp.Edges.Count > 0)
        {
            var (edgeId, edgeFlags) = comp.Edges[^1];
            RemoveEdge(uid, edgeId, ^1, edgeFlags, node: comp, edge: _nodeQuery.GetComponent(edgeId));
        }

        if (comp.GraphId is { } graphId)
            RemoveNode(graphId, uid, graph: _graphQuery.GetComponent(graphId), node: comp);
    }
}
