using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    /// <summary>
    /// Checks whether an edge exists between two nodes.
    /// </summary>
    /// <returns>True if an edge exists between <paramref name="nodeId"/> and <paramref name="edgeId"/>; False otherwise.</returns>
    public bool HasEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null)
    {
        if (!Resolve(nodeId, ref node))
            return false;

        return GetEdgeIndex(node, edgeId) is { };
    }

    /// <summary>
    /// Attempts to add an externally managed edge between two nodes. Fails if doing so is impossible or if such an edge already exists.
    /// </summary>
    /// <remarks>
    /// Will override any automatically managed edge between the two nodes.
    /// </remarks>
    /// <returns>True if the edge was added/enforced; False otherwise.</returns>
    public bool TryAddEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags = Edge.DefaultFlags, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (GetEdgeIndex(node, edgeId) is { } edgeIdx)
        {
            var (_, edgeFlags) = node.Edges[edgeIdx];
            if ((edgeFlags & EdgeFlags.Manual) != EdgeFlags.None)
                return false;

            SetEdge(nodeId, edgeId, edgeIdx, flags | EdgeFlags.Manual, edgeFlags, node, edge);
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
        {
            SetEdge(nodeId, edgeId, edgeIdx, edgeFlags & ~EdgeFlags.Manual, edgeFlags, node, edge);
            if ((node.Flags & NodeFlags.Edges) == NodeFlags.None)
                QueueEdgeUpdate(nodeId, node);
        }
        else
            RemoveEdge(nodeId, edgeId, edgeIdx, edgeFlags, node, edge);

        return true;
    }

    public bool TrySetEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (GetEdgeIndex(node, edgeId) is not { } edgeIdx)
            return false;

        var oldFlags = node.Edges[edgeIdx].Flags;
        if (((flags ^ oldFlags) & ~EdgeFlags.Manual) == EdgeFlags.None)
            return false;

        SetEdge(nodeId, edgeId, edgeIdx, flags | EdgeFlags.Manual, oldFlags, node, edge);
        return true;
    }

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

    protected void AddEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        DebugTools.Assert(nodeId != edgeId, $"Graph node {ToPrettyString(nodeId)} attempted to form an edge with itself.");

        AddHalfEdge(nodeId, edgeId, flags, node, edge);
        AddHalfEdge(edgeId, nodeId, flags, edge, node);

        var nodeEv = new EdgeAddedEvent(nodeId, edgeId, flags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeAddedEvent(edgeId, nodeId, flags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    protected void RemoveEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        RemoveHalfEdge(nodeId, edgeId, idx, flags, node, edge);
        RemoveHalfEdge(edgeId, edgeId, GetEdgeIndex(edge, nodeId)!.Value, flags, edge, node);

        var nodeEv = new EdgeRemovedEvent(nodeId, edgeId, flags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeRemovedEvent(edgeId, nodeId, flags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    protected void SetEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        SetHalfEdge(nodeId, edgeId, idx, newFlags, oldFlags, node, edge);
        SetHalfEdge(edgeId, nodeId, GetEdgeIndex(edge, nodeId)!.Value, newFlags, oldFlags, edge, node);

        var nodeEv = new EdgeChangedEvent(nodeId, edgeId, newFlags, oldFlags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeChangedEvent(edgeId, nodeId, newFlags, oldFlags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    protected void AddHalfEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        node.Edges.Add(new Edge(edgeId, flags));
        OnHalfEdgeChanged(nodeId, edgeId, flags, Edge.NullFlags, node, edge);
    }

    protected void RemoveHalfEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        node.Edges.RemoveSwap(idx.IsFromEnd ? node.Edges.Count - idx.Value : idx.Value);
        OnHalfEdgeChanged(nodeId, edgeId, Edge.NullFlags, oldFlags, node, edge);
    }

    protected void SetHalfEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        if (newFlags == oldFlags)
            return;

        node.Edges[idx] = new Edge(edgeId, newFlags);
        OnHalfEdgeChanged(nodeId, edgeId, newFlags, oldFlags, node, edge);
    }

    protected void OnHalfEdgeChanged(EntityUid nodeId, EntityUid _, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        var deltaFlags = newFlags ^ oldFlags;
        if (deltaFlags == EdgeFlags.None)
            return;

        // Handle changing whether the edge can be merged over.
        if ((deltaFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
        {
            if ((newFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
            {
                // Could be merged over, now can't.
                --node.NumMergeableEdges;
                if (node.NumMergeableEdges <= 0)
                    ClearMerge(nodeId, node);

                if (edge.GraphId is { } graphId1 && graphId1 == node.GraphId)
                    MarkSplit(nodeId, node);
                return;
            }
            else
            {
                // Couldn't be merged over, now can.
                ++node.NumMergeableEdges;
                if (edge.GraphId is not { } graphId2 || graphId2 == node.GraphId || node.GraphProto != edge.GraphProto || !GraphQuery.TryGetComponent(graphId2, out var graph))
                    return;

                if (node.NumMergeableEdges == 1)
                {
                    if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                        ClearSplit(nodeId, node);

                    AddNode(graphId2, nodeId, graph: graph, node: node);
                }
                else
                    MarkMerge(nodeId, node);
            }
        }
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

            var newFlags = EdgeFlags.None;
            if (newEdges?.Remove(edgeId, out var outFlags) == true)
                newFlags |= outFlags | EdgeFlags.Auto;

            var edge = NodeQuery.GetComponent(edgeId);
            if (CheckEdge(edgeId, nodeId, out var inFlags, edge, node))
                newFlags |= inFlags | EdgeFlags.Auto;

            // Manually set edges shouldn't be messed with by the autolinker.
            if ((edgeFlags & EdgeFlags.Manual) != EdgeFlags.None)
                newFlags = edgeFlags | (newFlags & EdgeFlags.Auto);
            else if ((newFlags & EdgeFlags.Auto) == EdgeFlags.None)
            {
                RemoveEdge(nodeId, edgeId, i, edgeFlags, node, edge);
                continue;
            }

            if ((newFlags ^ edgeFlags) != EdgeFlags.None)
                SetEdge(nodeId, edgeId, i, newFlags, edgeFlags, node, edge);
        }

        if (newEdges is null || newEdges.Count <= 0)
            return;

        foreach (var (edgeId, edgeFlags) in newEdges)
        {
            if (!NodeQuery.TryGetComponent(edgeId, out var edge))
            {
                Log.Error($"Autolinker attempted to form an edge between graph node {ToPrettyString(nodeId)} and non-node {ToPrettyString(edgeId)}.");
                continue;
            }

            var newFlags = edgeFlags | EdgeFlags.Auto;
            if (CheckEdge(edgeId, nodeId, out var inFlags, edge, node))
                newFlags |= inFlags;

            AddEdge(nodeId, edgeId, newFlags, node, edge);
        }
    }

    protected bool CheckEdge(EntityUid nodeId, EntityUid edgeId, out EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        var checkEv = new CheckEdgeEvent(nodeId, edgeId, node, edge);
        RaiseLocalEvent(nodeId, ref checkEv);
        flags = checkEv.Flags;
        return checkEv.Wanted;
    }
}
