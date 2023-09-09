using Content.Shared.Nodes.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    protected virtual void OnComponentShutdown(EntityUid uid, GraphNodeComponent comp, ComponentShutdown args)
    {
        while (comp.Edges.FirstOrNull() is { } edgeId)
        {
            RemoveEdge(uid, edgeId, node: comp, edge: NodeQuery.GetComponent(edgeId));
        }
    }
}
