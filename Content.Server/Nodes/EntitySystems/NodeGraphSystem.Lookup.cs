using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Gets the proxy node attached to a polynode by a certain key.
    /// </summary>
    public bool TryGetProxyNode(EntityUid polyId, string proxyKey, out EntityUid proxyId, PolyNodeComponent? poly = null)
    {
        proxyId = EntityUid.Invalid;
        if (!_polyQuery.Resolve(polyId, ref poly))
            return false;

        return poly.ProxyNodes.TryGetValue(proxyKey, out proxyId);
    }

    /// <summary>
    /// Gets the entity acting as a host for a given node.
    /// If the node is a proxy node this is the polynode it is attached to, otherwise it's just itself.
    /// </summary>
    public EntityUid GetNodeHost(EntityUid nodeId, GraphNodeComponent? node = null, ProxyNodeComponent? proxy = null)
    {
        if (!_nodeQuery.Resolve(nodeId, ref node))
            return EntityUid.Invalid;

        if (_proxyQuery.Resolve(nodeId, ref proxy, logMissing: false) && proxy.ProxyFor is { } proxyFor)
            return proxyFor;

        return nodeId;
    }


    /// <summary>
    /// Finds the uppermost graph of a tree of node graphs.
    /// </summary>
    public EntityUid GetRootGraph(EntityUid uid, GraphNodeComponent? node = null)
    {
        while (_nodeQuery.Resolve(uid, ref node))
        {
            if (node.GraphId is not { } tmp)
                break;

            uid = tmp;
        }

        return _graphQuery.HasComponent(uid) ? uid : EntityUid.Invalid;
    }

    /// <summary>
    /// Enumerates all of the non-graph descendant nodes of a graph.
    /// </summary>
    public IEnumerable<EntityUid> EnumerateLeafNodes(EntityUid graphId, NodeGraphComponent? graph = null)
    {
        if (!_graphQuery.Resolve(graphId, ref graph))
            yield break;

        foreach (var nodeId in graph.Nodes)
        {
            if (!_graphQuery.TryGetComponent(nodeId, out var nodeGraph))
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
