using Content.Server.Nodes.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Returns an enumerable over all extant graphs of a given type.
    /// </summary>
    public IEnumerable<EntityUid> GetGraphsByType(string graphProto)
    {
        return _graphsByProto.TryGetValue(graphProto, out var graphs) ? graphs : new();
    }

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

    /// <summary>Enumerates all of the nodes associated with an entity.</summary>
    public IEnumerable<(EntityUid NodeId, GraphNodeComponent Node)> EnumerateNodes(EntityUid nodeOrPolyId, GraphNodeComponent? node = null, PolyNodeComponent? poly = null)
    {
        if (_nodeQuery.Resolve(nodeOrPolyId, ref node))
            yield return (nodeOrPolyId, node);

        if (_polyQuery.Resolve(nodeOrPolyId, ref poly))
        {
            foreach (var proxyId in poly.ProxyNodes.Values)
            {
                if (proxyId == nodeOrPolyId)
                    continue; // Already handled above.

                if (_nodeQuery.TryGetComponent(proxyId, out var proxyNode))
                    yield return (proxyId, proxyNode);
            }
        }
    }

    /// <summary>Enumerates all of the graphs associated with an entity.</summary>
    public IEnumerable<(EntityUid GraphId, NodeGraphComponent Graph)> EnumerateGraphs(EntityUid nodeOrPolyId, GraphNodeComponent? node = null, PolyNodeComponent? poly = null)
    {
        foreach (var (_, enumNode) in EnumerateNodes(nodeOrPolyId, node, poly))
        {
            if (enumNode.GraphId is { } graphId && _graphQuery.TryGetComponent(graphId, out var graph))
                yield return (graphId, graph);
        }
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


    /// <summary>
    /// Enumerates all of the nodes anchored to a tile.
    /// </summary>
    public IEnumerable<EntityUid> GetAnchoredNodesOnTile(MapGridComponent grid, Vector2i tileIndices)
    {
        foreach (var entityUid in grid.GetAnchoredEntities(tileIndices))
        {
            foreach (var (nodeId, _) in EnumerateNodes(entityUid))
                yield return nodeId;
        }
    }

    /// <summary>
    /// Enumerates all of the nodes anchored to a tile.
    /// </summary>
    public IEnumerable<(EntityUid, Direction)> GetAnchoredNodesInDirs(MapGridComponent grid, Vector2i tileIndices, Direction[] dirs)
    {
        foreach (var dir in dirs)
        {
            if (dir == Direction.Invalid)
            {
                foreach (var nodeId in GetAnchoredNodesOnTile(grid, tileIndices))
                    yield return (nodeId, dir);

                continue;
            }

            foreach (var nodeId in GetAnchoredNodesOnTile(grid, tileIndices.Offset(dir)))
                yield return (nodeId, dir);
        }
    }

    /// <inheritdoc cref="GetAnchoredNodesInDirs" />
    public IEnumerable<(EntityUid, Direction)> GetAnchoredNodesInDir(MapGridComponent grid, Vector2i tileIndices, params Direction[] dir)
        => GetAnchoredNodesInDirs(grid, tileIndices, dir);
}
