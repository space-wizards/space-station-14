using Content.Shared.Nodes.Components;

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

    public bool TryAddEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (nodeId == edgeId)
            return false; // TODO: Check deletion status.

        if (node.Edges.Contains(edgeId))
            return false;

        AddHalfEdge(nodeId, edgeId, node, edge);
        AddHalfEdge(edgeId, nodeId, edge, node);

        // TODO: Raise events.
        return true;
    }

    public bool TryRemoveEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (!node.Edges.Contains(edgeId))
            return false;

        RemoveHalfEdge(nodeId, edgeId, node, edge);
        RemoveHalfEdge(edgeId, nodeId, edge, node);

        // TODO: Raise events.
        return true;
    }

    protected void AddHalfEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent node, GraphNodeComponent edge)
    {
        Dirty(nodeId, node);
        node.Edges.Add(edgeId);

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

    protected void RemoveHalfEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent node, GraphNodeComponent edge)
    {
        Dirty(nodeId, node);
        node.Edges.Remove(edgeId);

        if (node.Edges.Count <= 0)
            ClearMerge(nodeId, node);

        if (edge.GraphId is { } graphId && graphId == node.GraphId)
            MarkSplit(nodeId, node);
    }

    protected void UpdateEdges(EntityUid nodeId, GraphNodeComponent node)
    {
        if ((node.Flags & NodeFlags.Edges) != NodeFlags.None)
            ClearEdgeUpdate(nodeId, node);

        // TODO: Handle recalculating node edges.
    }
}
