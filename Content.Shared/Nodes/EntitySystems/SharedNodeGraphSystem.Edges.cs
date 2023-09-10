using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    public void QueueEdgeUpdate(EntityUid nodeId, GraphNodeComponent? node = null)
    {
        if (!Resolve(nodeId, ref node))
            return;

        if ((node.Flags & NodeFlags.Edges) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Edges;
        QueuedEdgeUpdates.Add(nodeId);
    }

    public void ClearEdgeUpdate(EntityUid nodeId, GraphNodeComponent? node = null)
    {
        if (!Resolve(nodeId, ref node))
            return;

        if ((node.Flags & NodeFlags.Edges) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Edges;
        QueuedEdgeUpdates.Remove(nodeId);
    }

    public bool HasEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null)
    {
        if (!Resolve(nodeId, ref node))
            return false;

        return GetEdgeIndex(node, edgeId) is { };
    }

    protected static Index? GetEdgeIndex(GraphNodeComponent node, EntityUid checkId)
    {
        for (var i = 0; i < node.Edges.Count; ++i)
        {
            var (edgeId, _) = node.Edges[i];
            if (edgeId == checkId)
                return i;
        }

        return null;
    }

    public bool TryAddEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags = EdgeFlags.None, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (GetEdgeIndex(node, edgeId) is { } edgeIdx)
        {
            var (_, edgeFlags) = node.Edges[edgeIdx];
            if ((edgeFlags & EdgeFlags.Manual) != EdgeFlags.None)
                return false;

            node.Edges[edgeIdx] = new Edge(edgeId, edgeFlags | flags | EdgeFlags.Manual);
        }
        else
            AddEdge(nodeId, edgeId, flags | EdgeFlags.Manual, node, edge);

        return true;
    }

    public bool TryRemoveEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (GetEdgeIndex(node, edgeId) is not { } edgeIdx)
            return false;

        var (_, edgeFlags) = node.Edges[edgeIdx];
        if ((edgeFlags & EdgeFlags.Manual) == EdgeFlags.None)
            return false;

        if ((edgeFlags & EdgeFlags.Auto) != EdgeFlags.None)
            node.Edges[edgeIdx] = new Edge(edgeId, edgeFlags & ~EdgeFlags.Manual);
        else
            RemoveEdge(nodeId, edgeId, edgeIdx, node, edge);

        return true;
    }

    protected void AddEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        DebugTools.Assert(nodeId != edgeId, $"Graph node {ToPrettyString(nodeId)} attempted to form an edge with itself.");

        AddHalfEdge(nodeId, edgeId, flags, node, edge);
        AddHalfEdge(edgeId, nodeId, flags, edge, node);

        var nodeEv = new EdgeAddedEvent(nodeId, edgeId, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeAddedEvent(edgeId, nodeId, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    protected void RemoveEdge(EntityUid nodeId, EntityUid edgeId, Index idx, GraphNodeComponent node, GraphNodeComponent edge)
    {
        RemoveHalfEdge(nodeId, idx, node, edge);
        RemoveHalfEdge(edgeId, GetEdgeIndex(edge, nodeId)!.Value, edge, node);

        var nodeEv = new EdgeRemovedEvent(nodeId, edgeId, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeRemovedEvent(edgeId, nodeId, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    protected void AddHalfEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        Dirty(nodeId, node);
        node.Edges.Add(new Edge(edgeId, flags));

        if (edge.GraphId is not { } graphId || graphId == node.GraphId || node.GraphProto != edge.GraphProto || !GraphQuery.TryGetComponent(graphId, out var graph))
            return;

        if (node.Edges.Count == 1)
        {
            if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                ClearSplit(nodeId, node);

            AddNode(graphId, nodeId, graph: graph, node: node);
        }
        else
            MarkMerge(nodeId, node);
    }

    protected void RemoveHalfEdge(EntityUid nodeId, Index idx, GraphNodeComponent node, GraphNodeComponent edge)
    {
        Dirty(nodeId, node);
        node.Edges.RemoveSwap(idx.IsFromEnd ? node.Edges.Count - idx.Value : idx.Value);

        if (node.Edges.Count <= 0)
            ClearMerge(nodeId, node);

        if (edge.GraphId is { } graphId && graphId == node.GraphId)
            MarkSplit(nodeId, node);
    }

    protected void UpdateEdges(EntityUid nodeId, GraphNodeComponent node)
    {
        if ((node.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(nodeId, node);

        var updateEv = new UpdateEdgesEvent(nodeId, node);
        RaiseLocalEvent(nodeId, ref updateEv);

        var newEdges = updateEv.Edges;

        // For loop because the edges will be modified mid-iteration.
        for (var i = node.Edges.Count - 1; i >= 0; --i)
        {
            var (edgeId, edgeFlags) = node.Edges[i];

            if (newEdges?.Remove(edgeId) == true)
            {
                node.Edges[i] = new Edge(edgeId, edgeFlags | EdgeFlags.Auto);
                continue;
            }

            var edge = NodeQuery.GetComponent(edgeId);
            if (CheckEdge(edgeId, nodeId, edge, node))
            {
                node.Edges[i] = new Edge(edgeId, edgeFlags | EdgeFlags.Auto);
                continue;
            }

            if ((edgeFlags & EdgeFlags.Manual) != EdgeFlags.None)
                node.Edges[i] = new Edge(edgeId, edgeFlags & ~EdgeFlags.Auto);
            else
                RemoveEdge(nodeId, edgeId, i, node, edge);
        }

        if (newEdges is null || newEdges.Count <= 0)
            return;

        foreach (var edgeId in newEdges)
        {
            if (!NodeQuery.TryGetComponent(edgeId, out var edge))
            {
                Log.Error($"Autolinker attempted to form an edge between graph node {ToPrettyString(nodeId)} and non-node {ToPrettyString(edgeId)}.");
                continue;
            }

            AddEdge(nodeId, edgeId, EdgeFlags.Auto, node, edge);
        }
    }

    protected bool CheckEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent node, GraphNodeComponent edge)
    {
        var checkEv = new CheckEdgeEvent(nodeId, edgeId, node, edge);
        RaiseLocalEvent(nodeId, ref checkEv);
        return checkEv.Wanted;
    }
}
