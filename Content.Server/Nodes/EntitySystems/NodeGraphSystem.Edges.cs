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
    public bool HasEdge(Entity<GraphNodeComponent?> from, EntityUid to)
    {
        if (!_nodeQuery.Resolve(from.Owner, ref from.Comp))
            return false;

        return GetEdgeIndex(from.Comp, to) is { };
    }

    /// <summary>
    /// Gets the edge flags for an edge between two nodes if such exists.
    /// </summary>
    /// <returns>The edge flags for some edge between <paramref name="nodeId"/> and <paramref name="edgeId"/>; null if no such edge exists.</returns>
    public EdgeFlags? GetEdgeOrNull(Entity<GraphNodeComponent?> from, EntityUid to)
    {
        if (!_nodeQuery.Resolve(from.Owner, ref from.Comp))
            return null;

        return GetEdgeIndex(from.Comp, to) is { } index ? from.Comp.Edges[index].Flags : null;
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

            SetEdge((nodeId, node), (edgeId, edge), edgeIdx, flags | EdgeFlags.Manual, edgeFlags);
        }
        else
            AddEdge((nodeId, node), (edgeId, edge), flags | EdgeFlags.Manual);

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
            SetEdge((nodeId, node), (edgeId, edge), edgeIdx, edgeFlags & ~EdgeFlags.Manual, edgeFlags);
            if ((node.Flags & NodeFlags.Edges) == NodeFlags.None)
                QueueEdgeUpdate(nodeId, node);
        }
        else
            RemoveEdge((nodeId, node), (edgeId, edge), edgeIdx, edgeFlags);

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
            AddEdge((nodeId, node), (edgeId, edge), flags | EdgeFlags.Manual);
            return true;
        }

        var oldFlags = node.Edges[edgeIdx].Flags;
        if ((oldFlags & EdgeFlags.Manual) != EdgeFlags.None && ((flags ^ oldFlags) & ~EdgeFlags.SourceMask) == EdgeFlags.None)
            return true;

        SetEdge((nodeId, node), (edgeId, edge), edgeIdx, flags | EdgeFlags.Manual | (oldFlags & EdgeFlags.SourceMask), oldFlags);
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
    private void AddEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, EdgeFlags flags)
    {
        DebugTools.Assert(from.Owner != to.Owner, $"Graph node {ToPrettyString(from)} attempted to form an edge with itself.");

        AddHalfEdge(from, to, flags);
        AddHalfEdge(to, from, flags);

        var nodeEv = new EdgeAddedEvent(from, to, flags);
        RaiseLocalEvent(from, ref nodeEv);
        var edgeEv = new EdgeAddedEvent(to, from, flags);
        RaiseLocalEvent(to, ref edgeEv);
    }

    /// <summary>
    /// Removes an edge between two nodes.
    /// </summary>
    private void RemoveEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, Index idx, EdgeFlags flags)
    {
        RemoveHalfEdge(from, to, idx, flags);
        RemoveHalfEdge(to, from, GetEdgeIndex(to, from.Owner)!.Value, flags);

        var nodeEv = new EdgeRemovedEvent(from, to, flags);
        RaiseLocalEvent(from, ref nodeEv);
        var edgeEv = new EdgeRemovedEvent(to, from, flags);
        RaiseLocalEvent(to, ref edgeEv);
    }

    /// <summary>
    /// Changes the state of an edge between two nodes.
    /// </summary>
    private void SetEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags)
    {
        if (newFlags == oldFlags)
            return;

        var newEdgeFlags = newFlags.Invert();
        var oldEdgeFlags = oldFlags.Invert();
        SetHalfEdge(from, to, idx, newFlags, oldFlags);
        SetHalfEdge(to, from, GetEdgeIndex(to, from.Owner)!.Value, newEdgeFlags, oldEdgeFlags);

        var nodeEv = new EdgeChangedEvent(from, to, newFlags, oldFlags);
        RaiseLocalEvent(from, ref nodeEv);
        var edgeEv = new EdgeChangedEvent(to, from, newEdgeFlags, oldEdgeFlags);
        RaiseLocalEvent(to, ref edgeEv);
    }

    /// <summary>
    /// Handles one half of the state changes involved in adding an edge between two nodes.
    /// </summary>
    private void AddHalfEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, EdgeFlags flags)
    {
        from.Comp.Edges.Add(new Edge(to.Owner, flags));
        Dirty(from);

        OnHalfEdgeChanged(from, to, flags, Edge.NullFlags);
    }

    /// <summary>
    /// Handles one half of the state changes involved in removing an edge between two nodes.
    /// </summary>
    private void RemoveHalfEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, Index idx, EdgeFlags oldFlags)
    {
        from.Comp.Edges.RemoveSwap(idx.IsFromEnd ? from.Comp.Edges.Count - idx.Value : idx.Value);
        Dirty(from);

        OnHalfEdgeChanged(from, to, Edge.NullFlags, oldFlags);
    }

    /// <summary>
    /// Handles one half of the state changes involved in changing the state of an edge between two nodes.
    /// </summary>
    private void SetHalfEdge(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, Index idx, EdgeFlags newFlags, EdgeFlags oldFlags)
    {
        from.Comp.Edges[idx] = new Edge(to.Owner, newFlags);
        Dirty(from);

        OnHalfEdgeChanged(from, to, newFlags, oldFlags);
    }

    /// <summary>
    /// Handles general node state changes triggered when an edge of that node changes.
    /// </summary>
    private void OnHalfEdgeChanged(Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to, EdgeFlags newFlags, EdgeFlags oldFlags)
    {
        var deltaFlags = newFlags ^ oldFlags;
        if (deltaFlags == EdgeFlags.None)
            return;

        // Handle changing whether the edge can be merged over.
        var (fromId, fromComp) = from;
        var (toId, toComp) = to;
        if ((deltaFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
        {
            if ((newFlags & EdgeFlags.NoMerge) != EdgeFlags.None)
            {
                // Could be merged over, now can't.
                --fromComp.NumMergeableEdges;
                if (fromComp.NumMergeableEdges <= 0)
                    ClearMerge(from);

                if (toComp.GraphId is { } graphId1 && graphId1 == fromComp.GraphId)
                    MarkSplit(from);
                return;
            }
            else
            {
                // Couldn't be merged over, now can.
                ++fromComp.NumMergeableEdges;
                if (toComp.GraphId is not { } graphId2 || graphId2 == fromComp.GraphId || fromComp.GraphProto != toComp.GraphProto)
                    return;

                if (fromComp.NumMergeableEdges == 1)
                {
                    if ((fromComp.Flags & NodeFlags.Split) != NodeFlags.None)
                        ClearSplit(from);

                    AddNode(graph: (graphId2, _graphQuery.GetComponent(graphId2)), node: from);
                }
                else
                    MarkMerge(from);
            }
        }
    }


    /// <summary>
    /// Updates the autolinker-generated edges associated with a node.
    /// </summary>
    private void UpdateEdges(Entity<GraphNodeComponent> node)
    {
        if ((node.Comp.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(node);

        // Cache commonly derived autolinker values:
        var hostId = GetNodeHost((node.Owner, node.Comp, null));
        Entity<TransformComponent?> nodeHost = (GetNodeHost((node.Owner, node.Comp, null)), null);
        Entity<MapGridComponent>? nodeGrid =
            _xformQuery.TryGetComponent(nodeHost.Owner, out nodeHost.Comp) &&
            TryComp(nodeHost.Comp.GridUid, out MapGridComponent? hostGrid)
            ? (nodeHost.Comp.GridUid.Value, hostGrid) : null;

        // Collect edges the autolinkers want to have:
        var updateEv = new UpdateEdgesEvent(node, nodeHost, nodeGrid);
        RaiseLocalEvent(node, ref updateEv);
        var newEdges = updateEv.Edges;

        // Figure out what edges we have that we shouldn't.
        // For loop because the edges will be modified mid-iteration.
        for (var i = node.Comp.Edges.Count - 1; i >= 0; --i)
        {
            var (edgeId, edgeFlags) = node.Comp.Edges[i];

            var newFlags = EdgeFlags.None;
            if (newEdges?.Remove(edgeId, out var outFlags) == true)
                newFlags |= outFlags | EdgeFlags.Auto | EdgeFlags.Out;

            var edge = _nodeQuery.GetComponent(edgeId);
            Entity<TransformComponent?> edgeHost = (GetNodeHost((edgeId, edge, null)), null);
            Entity<MapGridComponent>? edgeGrid =
                _xformQuery.TryGetComponent(edgeHost.Owner, out edgeHost.Comp) &&
                TryComp(edgeHost.Comp.GridUid, out MapGridComponent? otherGrid)
                ? (edgeHost.Comp.GridUid.Value, otherGrid) : null;

            if (CheckEdge((edgeId, edge), node, edgeHost, nodeHost, edgeGrid, nodeGrid, edgeFlags.Invert(), out var inFlags))
                newFlags |= inFlags | EdgeFlags.Auto | EdgeFlags.In;

            // Manually set edges shouldn't be messed with by the autolinker.
            if ((edgeFlags & EdgeFlags.Manual) != EdgeFlags.None)
                newFlags = edgeFlags | (newFlags & EdgeFlags.Auto);
            else if ((newFlags & EdgeFlags.Auto) == EdgeFlags.None)
            {
                RemoveEdge(node, (edgeId, edge), i, edgeFlags);
                continue;
            }

            if ((newFlags ^ edgeFlags) != EdgeFlags.None)
                SetEdge(node, (edgeId, edge), i, newFlags, edgeFlags);
        }

        if (newEdges is null || newEdges.Count <= 0)
            return;

        // Add missing edges.
        foreach (var (edgeId, edgeFlags) in newEdges)
        {
            if (!_nodeQuery.TryGetComponent(edgeId, out var edge))
            {
                Log.Error($"Autolinker attempted to form an edge between graph node {ToPrettyString(node)} and non-node {ToPrettyString(edgeId)}.");
                continue;
            }

            var newFlags = edgeFlags | EdgeFlags.Auto | EdgeFlags.Out;
            Entity<TransformComponent?> edgeHost = (GetNodeHost((edgeId, edge, null)), null);
            Entity<MapGridComponent>? edgeGrid =
                _xformQuery.TryGetComponent(edgeHost.Owner, out edgeHost.Comp) &&
                TryComp(edgeHost.Comp.GridUid, out MapGridComponent? otherGrid)
                ? (edgeHost.Comp.GridUid.Value, otherGrid) : null;

            if (CheckEdge((edgeId, edge), node, edgeHost, nodeHost, edgeGrid, nodeGrid, edgeFlags.Invert(), out var inFlags))
                newFlags |= inFlags | EdgeFlags.In;

            AddEdge(node, (edgeId, edge), newFlags);
        }
    }

    /// <summary>
    /// Checks whether a node wants an edge to exist between it and another node.
    /// </summary>
    /// <returns>True if the node wants the edge to exist; False otherwise.</returns>
    private bool CheckEdge(
        Entity<GraphNodeComponent> from, Entity<GraphNodeComponent> to,
        Entity<TransformComponent?> fromHost, Entity<TransformComponent?> toHost,
        Entity<MapGridComponent>? fromGrid, Entity<MapGridComponent>? toGrid,
        EdgeFlags? oldFlags, out EdgeFlags flags
    )
    {
        var checkEv = new CheckEdgeEvent(from, to, fromHost, toHost, fromGrid, toGrid, oldFlags);
        RaiseLocalEvent(from, ref checkEv);
        flags = checkEv.Flags ?? Edge.DefaultFlags;
        return checkEv.Wanted;
    }
}
