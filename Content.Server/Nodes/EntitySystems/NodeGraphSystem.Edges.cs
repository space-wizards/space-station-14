using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Checks whether an edge exists between two nodes.
    /// </summary>
    /// <returns>True if an edge exists between <paramref name="nodeId"/> and <paramref name="edgeId"/>; False otherwise.</returns>
    public bool HasEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node))
            return false;

        return GetEdgeIndex(node, edgeId) is { };
    }

    /// <summary>
    /// Gets the edge flags for an edge between two nodes if such exists.
    /// </summary>
    /// <returns>The edge flags for some edge between <paramref name="nodeId"/> and <paramref name="edgeId"/>; null if no such edge exists.</returns>
    public EdgeFlags? GetEdgeOrNull(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node))
            return null;

        return GetEdgeIndex(node, edgeId) is { } index ? node.Edges[index].Flags : null;
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
        if (!_nodeQuery.Resolve(nodeId, ref node) || !_nodeQuery.Resolve(edgeId, ref edge))
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

    /// <summary>
    /// Attempts to remove an externally managed edge between two nodes. Fails if doing so is impossible or if no such edge exists.
    /// </summary>
    /// <remarks>
    /// Will fallback to autolinker edges through prompting an edge update if necessary.
    /// </remarks>
    /// <returns>True if the edge was removed/relaxed; False otherwise.</returns>
    public bool TryRemoveEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node) || !_nodeQuery.Resolve(edgeId, ref edge))
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

    /// <summary>
    /// Attempts to set the state of an edge between two nodes.
    /// </summary>
    /// <remarks>
    /// Will override the state of any manually created or autolinker edges that already exist between the nodes.
    /// </remarks>
    /// <returns>True if the edge was created/changed; False otherwise.</returns>
    public bool TrySetEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node) || !_nodeQuery.Resolve(edgeId, ref edge))
            return false;

        if (GetEdgeIndex(node, edgeId) is not { } edgeIdx)
        {
            AddEdge(nodeId, edgeId, flags | EdgeFlags.Manual, node, edge);
            return true;
        }

        var oldFlags = node.Edges[edgeIdx].Flags;
        if ((oldFlags & EdgeFlags.Manual) != EdgeFlags.None && ((flags ^ oldFlags) & ~EdgeFlags.SourceMask) == EdgeFlags.None)
            return true;

        SetEdge(nodeId, edgeId, edgeIdx, flags | EdgeFlags.Manual | (oldFlags & EdgeFlags.SourceMask), oldFlags, node, edge);
        return true;
    }

    /// <summary>
    /// Queues the node to have its autolinker edges recalculated on the next update tick.
    /// </summary>
    public void QueueEdgeUpdate(EntityUid nodeId, GraphNodeComponent? node = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node))
            return;

        if ((node.Flags & NodeFlags.Edges) != NodeFlags.None)
            return;

        node.Flags |= NodeFlags.Edges;
        _queuedEdgeUpdates.Add(nodeId);
    }

    /// <summary>
    /// Removes the node from the autolinker update queue.
    /// </summary>
    public void ClearEdgeUpdate(EntityUid nodeId, GraphNodeComponent? node = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node))
            return;

        if ((node.Flags & NodeFlags.Edges) == NodeFlags.None)
            return;

        node.Flags &= ~NodeFlags.Edges;
        _queuedEdgeUpdates.Remove(nodeId);
    }


    /// <summary>
    /// Gets the index of the nodes edge to the given other node.
    /// </summary>
    /// <returns>The index (for <see cref="GraphNodeComponent.Edges"/>) of the edge to the given node; null if such an edge does not exist.</returns>
    private static Index? GetEdgeIndex(GraphNodeComponent node, EntityUid checkId)
    {
        for (var i = 0; i < node.Edges.Count; ++i)
        {
            var (edgeId, _) = node.Edges[i];
            if (edgeId == checkId)
                return i;
        }

        return null;
    }

    /// <summary>
    /// Adds an edge between two nodes.
    /// </summary>
    private void AddEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        DebugTools.Assert(nodeId != edgeId, $"Graph node {ToPrettyString(nodeId)} attempted to form an edge with itself.");

        AddHalfEdge(nodeId, edgeId, flags, node, edge);
        AddHalfEdge(edgeId, nodeId, flags, edge, node);

        var nodeEv = new EdgeAddedEvent(nodeId, edgeId, flags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeAddedEvent(edgeId, nodeId, flags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    /// <summary>
    /// Removes an edge between two nodes.
    /// </summary>
    private void RemoveEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        RemoveHalfEdge(nodeId, edgeId, idx, flags, node, edge);
        RemoveHalfEdge(edgeId, edgeId, GetEdgeIndex(edge, nodeId)!.Value, flags, edge, node);

        var nodeEv = new EdgeRemovedEvent(nodeId, edgeId, flags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeRemovedEvent(edgeId, nodeId, flags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    /// <summary>
    /// Changes the state of an edge between two nodes.
    /// </summary>
    private void SetEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        if (newFlags == oldFlags)
            return;

        var newEdgeFlags = newFlags.Invert();
        var oldEdgeFlags = oldFlags.Invert();
        SetHalfEdge(nodeId, edgeId, idx, newFlags, oldFlags, node, edge);
        SetHalfEdge(edgeId, nodeId, GetEdgeIndex(edge, nodeId)!.Value, newEdgeFlags, oldEdgeFlags, edge, node);

        var nodeEv = new EdgeChangedEvent(nodeId, edgeId, newFlags, oldFlags, node, edge);
        RaiseLocalEvent(nodeId, ref nodeEv);
        var edgeEv = new EdgeChangedEvent(edgeId, nodeId, newEdgeFlags, oldEdgeFlags, edge, node);
        RaiseLocalEvent(edgeId, ref edgeEv);
    }

    /// <summary>
    /// Handles one half of the state changes involved in adding an edge between two nodes.
    /// </summary>
    private void AddHalfEdge(EntityUid nodeId, EntityUid edgeId, EdgeFlags flags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        node.Edges.Add(new Edge(edgeId, flags));
        Dirty(nodeId, node);

        OnHalfEdgeChanged(nodeId, edgeId, flags, Edge.NullFlags, node, edge);
    }

    /// <summary>
    /// Handles one half of the state changes involved in removing an edge between two nodes.
    /// </summary>
    private void RemoveHalfEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        node.Edges.RemoveSwap(idx.IsFromEnd ? node.Edges.Count - idx.Value : idx.Value);
        Dirty(nodeId, node);

        OnHalfEdgeChanged(nodeId, edgeId, Edge.NullFlags, oldFlags, node, edge);
    }

    /// <summary>
    /// Handles one half of the state changes involved in changing the state of an edge between two nodes.
    /// </summary>
    private void SetHalfEdge(EntityUid nodeId, EntityUid edgeId, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
    {
        node.Edges[idx] = new Edge(edgeId, newFlags);
        Dirty(nodeId, node);

        OnHalfEdgeChanged(nodeId, edgeId, newFlags, oldFlags, node, edge);
    }

    /// <summary>
    /// Handles general node state changes triggered when an edge of that node changes.
    /// </summary>
    private void OnHalfEdgeChanged(EntityUid nodeId, EntityUid _, EdgeFlags newFlags, EdgeFlags oldFlags, GraphNodeComponent node, GraphNodeComponent edge)
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
                if (edge.GraphId is not { } graphId2 || graphId2 == node.GraphId || node.GraphProto != edge.GraphProto)
                    return;

                if (node.NumMergeableEdges == 1)
                {
                    if ((node.Flags & NodeFlags.Split) != NodeFlags.None)
                        ClearSplit(nodeId, node);

                    AddNode(graphId2, nodeId, graph: _graphQuery.GetComponent(graphId2), node: node);
                }
                else
                    MarkMerge(nodeId, node);
            }
        }
    }


    /// <summary>
    /// Updates the autolinker-generated edges associated with a node.
    /// </summary>
    private void UpdateEdges(EntityUid nodeId, GraphNodeComponent node)
    {
        if ((node.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(nodeId, node);

        // Cache commonly derived autolinker values:
        var hostId = GetNodeHost(nodeId, node);
        var hostXform = _xformQuery.GetComponent(hostId);
        _mapMan.TryGetGrid(hostXform.GridUid, out var hostGrid);

        // Collect edges the autolinkers want to have:
        var updateEv = new UpdateEdgesEvent(nodeId, hostId, node, hostXform, hostGrid);
        RaiseLocalEvent(nodeId, ref updateEv);
        var newEdges = updateEv.Edges;

        // Figure out what edges we have that we shouldn't.
        // For loop because the edges will be modified mid-iteration.
        for (var i = node.Edges.Count - 1; i >= 0; --i)
        {
            var (edgeId, edgeFlags) = node.Edges[i];

            var newFlags = EdgeFlags.None;
            if (newEdges?.Remove(edgeId, out var outFlags) == true)
                newFlags |= outFlags | EdgeFlags.Auto | EdgeFlags.Out;

            var edge = _nodeQuery.GetComponent(edgeId);
            var edgeHostId = GetNodeHost(edgeId, edge);
            var edgeHostXform = _xformQuery.GetComponent(edgeHostId);
            _mapMan.TryGetGrid(edgeHostXform.GridUid, out var edgeHostGrid);

            if (CheckEdge(
                edgeId, nodeId, edgeHostId, hostId, edgeFlags.Invert(), out var inFlags,
                node: edge, nodeHostXform: edgeHostXform, nodeHostGrid: edgeHostGrid,
                edge: node, edgeHostXform: hostXform, edgeHostGrid: hostGrid))
                newFlags |= inFlags | EdgeFlags.Auto | EdgeFlags.In;

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

        // Add missing edges.
        foreach (var (edgeId, edgeFlags) in newEdges)
        {
            if (!_nodeQuery.TryGetComponent(edgeId, out var edge))
            {
                Log.Error($"Autolinker attempted to form an edge between graph node {ToPrettyString(nodeId)} and non-node {ToPrettyString(edgeId)}.");
                continue;
            }

            var newFlags = edgeFlags | EdgeFlags.Auto | EdgeFlags.Out;
            var edgeHostId = GetNodeHost(edgeId, edge);
            var edgeHostXform = _xformQuery.GetComponent(edgeHostId);
            _mapMan.TryGetGrid(edgeHostXform.GridUid, out var edgeHostGrid);
            if (CheckEdge(
                edgeId, nodeId, edgeHostId, hostId, null, out var inFlags,
                node: edge, nodeHostXform: edgeHostXform, nodeHostGrid: edgeHostGrid,
                edge: node, edgeHostXform: hostXform, edgeHostGrid: hostGrid))
                newFlags |= inFlags | EdgeFlags.In;

            AddEdge(nodeId, edgeId, newFlags, node, edge);
        }
    }

    /// <summary>
    /// Checks whether a node wants an edge to exist between it and another node.
    /// </summary>
    /// <returns>True if the node wants the edge to exist; False otherwise.</returns>
    private bool CheckEdge(
        EntityUid nodeId, EntityUid edgeId, EntityUid nodeHostId, EntityUid edgeHostId, EdgeFlags? oldFlags, out EdgeFlags flags,
        GraphNodeComponent node, TransformComponent nodeHostXform, MapGridComponent? nodeHostGrid,
        GraphNodeComponent edge, TransformComponent edgeHostXform, MapGridComponent? edgeHostGrid
    )
    {
        var checkEv = new CheckEdgeEvent(
            nodeId, nodeHostId, edgeId, edgeHostId,
            node, nodeHostXform, nodeHostGrid,
            edge, edgeHostXform, edgeHostGrid,
            oldFlags
        );
        RaiseLocalEvent(nodeId, ref checkEv);
        flags = checkEv.Flags;
        return checkEv.Wanted;
    }
}
