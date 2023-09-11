using Content.Shared.Nodes.Components;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    public bool TryGetProxyNode(EntityUid polyId, string proxyKey, out EntityUid proxyId, PolyNodeComponent? poly = null)
    {
        proxyId = EntityUid.Invalid;
        if (!PolyQuery.Resolve(polyId, ref poly))
            return false;

        return poly.ProxyNodes.TryGetValue(proxyKey, out proxyId);
    }

    public EntityUid GetNodeHost(EntityUid nodeId, GraphNodeComponent? node = null, ProxyNodeComponent? proxy = null)
    {
        if (!Resolve(nodeId, ref node))
            return EntityUid.Invalid;

        if (ProxyQuery.Resolve(nodeId, ref proxy) && proxy.ProxyFor is { } proxyFor)
            return proxyFor;

        return nodeId;
    }

    public EntityUid GetRootGraph(EntityUid uid, GraphNodeComponent? node = null)
    {
        while (NodeQuery.Resolve(uid, ref node))
        {
            if (node.GraphId is not { } tmp)
                break;

            uid = tmp;
        }

        return GraphQuery.HasComponent(uid) ? uid : EntityUid.Invalid;
    }

    public IEnumerable<EntityUid> EnumerateLeafNodes(EntityUid graphId, NodeGraphComponent? graph = null)
    {
        if (!GraphQuery.Resolve(graphId, ref graph))
            yield break;

        foreach (var nodeId in graph.Nodes)
        {
            if (!GraphQuery.TryGetComponent(nodeId, out var nodeGraph))
            {
                yield return nodeId;
                continue;
            }

            foreach (var leafId in EnumerateLeafNodes(nodeId, nodeGraph))
            {
                yield return leafId;
            }
        }
    }
}
