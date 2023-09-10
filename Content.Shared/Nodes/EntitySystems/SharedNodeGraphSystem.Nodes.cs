using Content.Shared.Nodes.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    /// <summary>
    /// Marks a graph node as the location of a potential split in the graph.
    /// </summary>
    protected void MarkSplit(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Split;

        if (node.GraphId is { } graphId && Resolve(graphId, ref graph, logMissing: false)) // logMissing: false is temporary until a solution so server/client graphs is found (we don't want server graphs to be broadcast in whole to the client b/c they include information about a lot of entities outside of PVS range so the client doesn't have the graphs to resolve)
            QueueSplit(graphId, nodeId, graph);
    }

    /// <summary>
    /// Marks a graph node as the location of a potential merge between two graphs.
    /// </summary>
    protected void MarkMerge(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Merge) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Merge;

        if (node.GraphId is { } graphId && Resolve(graphId, ref graph, logMissing: false))
            QueueMerge(graphId, nodeId, graph);
    }

    /// <summary>
    /// Clears a graph node as the location of a potential split in the graph.
    /// </summary>
    protected void ClearSplit(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Split) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Split;

        if (node.GraphId is { } graphId && Resolve(graphId, ref graph, logMissing: false))
            CancelSplit(graphId, nodeId, graph);
    }

    /// <summary>
    /// Clears a graph node as the location of a potential merge between two or more graphs.
    /// </summary>
    protected void ClearMerge(EntityUid nodeId, GraphNodeComponent node, NodeGraphComponent? graph = null)
    {
        if ((node.Flags & NodeFlags.Merge) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Merge;

        if (node.GraphId is { } graphId && Resolve(graphId, ref graph, logMissing: false))
            CancelMerge(graphId, nodeId, graph);
    }

    protected virtual void OnComponentInit(EntityUid uid, GraphNodeComponent comp, ComponentInit args)
    {
        comp.NumMergeableEdges = 0;
        foreach (var (_, edgeFlags) in comp.Edges)
        {
            if ((edgeFlags & EdgeFlags.NoMerge) == EdgeFlags.None)
                ++comp.NumMergeableEdges;
        }
    }

    protected virtual void OnMapInit(EntityUid uid, GraphNodeComponent comp, MapInitEvent args)
    {
        if ((comp.Flags & NodeFlags.Init) == NodeFlags.None)
        {
            comp.Flags |= NodeFlags.Init;
            UpdateEdges(uid, comp);
        }

        if (comp.GraphId is not { })
            FloodSpawnGraph(uid, comp);
    }

    protected virtual void OnComponentShutdown(EntityUid uid, GraphNodeComponent comp, ComponentShutdown args)
    {
        if ((comp.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(uid, comp);

        while (comp.Edges.Count > 0)
        {
            var (edgeId, edgeFlags) = comp.Edges[^1];
            RemoveEdge(uid, edgeId, ^1, edgeFlags, node: comp, edge: NodeQuery.GetComponent(edgeId));
        }

        if (comp.GraphId is { } graphId && GraphQuery.TryGetComponent(graphId, out var graph))
            RemoveNode(graphId, uid, graph: graph, node: comp);
    }
}
