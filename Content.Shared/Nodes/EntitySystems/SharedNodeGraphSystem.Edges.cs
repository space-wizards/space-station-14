using Content.Shared.Nodes.Components;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    public bool AddEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (nodeId == edgeId)
            return false; // TODO: Check deletion status.

        if (node.Edges.Contains(edgeId))
            return false;

        node.Edges.Add(edgeId);
        edge.Edges.Add(nodeId);
        Dirty(nodeId, node);
        Dirty(edgeId, edge);
        return true;
    }

    public bool RemoveEdge(EntityUid nodeId, EntityUid edgeId, GraphNodeComponent? node = null, GraphNodeComponent? edge = null)
    {
        if (!Resolve(nodeId, ref node) || !Resolve(edgeId, ref edge))
            return false;

        if (!node.Edges.Contains(edgeId))
            return false;

        node.Edges.Remove(edgeId);
        edge.Edges.Remove(nodeId);
        Dirty(nodeId, node);
        Dirty(edgeId, edge);
        return true;
    }
}
