using Content.Server.Nodes.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    #region TryGetNode

    public bool TryGetNode(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent> node)
    {
        node = (EntityUid.Invalid, default!);
        if (key is null)
            node.Owner = host;
        else if (!_polyQuery.Resolve(host.Owner, ref host.Comp) || !host.Comp.ProxyNodes.TryGetValue(key, out node.Owner))
            return false;

        return _nodeQuery.TryGetComponent(node.Owner, out node.Comp!);
    }

    public bool TryGetNode<T>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T> node) where T : IComponent
    {
        if (!TryGetNode(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp, default!);
            return false;
        }

        node = (tmp.Owner, tmp.Comp, default!);
        return TryComp(node.Owner, out node.Comp2!);
    }

    public bool TryGetNode<T1, T2>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2> node)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (!TryGetNode<T1>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, default!);
        return TryComp(node.Owner, out node.Comp3!);
    }

    public bool TryGetNode<T1, T2, T3>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2, T3> node)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (!TryGetNode<T1, T2>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, default!);
        return TryComp(node.Owner, out node.Comp4!);
    }

    public bool TryGetNode<T1, T2, T3, T4>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2, T3, T4> node)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (!TryGetNode<T1, T2, T3>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, default!);
        return TryComp(node.Owner, out node.Comp5!);
    }

    public bool TryGetNode<T1, T2, T3, T4, T5>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2, T3, T4, T5> node)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (!TryGetNode<T1, T2, T3, T4>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, default!);
        return TryComp(node.Owner, out node.Comp6!);
    }

    public bool TryGetNode<T1, T2, T3, T4, T5, T6>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2, T3, T4, T5, T6> node)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (!TryGetNode<T1, T2, T3, T4, T5>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, tmp.Comp6, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, tmp.Comp6, default!);
        return TryComp(node.Owner, out node.Comp7!);
    }

    public bool TryGetNode<T1, T2, T3, T4, T5, T6, T7>(Entity<PolyNodeComponent?> host, string? key, out Entity<GraphNodeComponent, T1, T2, T3, T4, T5, T6, T7> node)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        if (!TryGetNode<T1, T2, T3, T4, T5, T6>(host, key, out var tmp))
        {
            node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, tmp.Comp6, tmp.Comp7, default!);
            return false;
        }

        node = (tmp, tmp.Comp1, tmp.Comp2, tmp.Comp3, tmp.Comp4, tmp.Comp5, tmp.Comp6, tmp.Comp7, default!);
        return TryComp(node.Owner, out node.Comp8!);
    }

    #endregion TryGetNode

    /// <summary>
    /// Gets the entity acting as a host for a given node.
    /// If the node is a proxy node this is the polynode it is attached to, otherwise it's just itself.
    /// </summary>
    public EntityUid GetNodeHost(Entity<GraphNodeComponent?, ProxyNodeComponent?> node)
    {
        if (!_nodeQuery.Resolve(node.Owner, ref node.Comp1))
            return EntityUid.Invalid;

        if (_proxyQuery.Resolve(node.Owner, ref node.Comp2, logMissing: false) && node.Comp2.ProxyFor is { } proxyFor)
            return proxyFor;

        return node.Owner;
    }

    /// <summary>Enumerates all of the nodes associated with an entity.</summary>
    public IEnumerable<Entity<GraphNodeComponent>> EnumerateNodes(Entity<GraphNodeComponent?, PolyNodeComponent?> host)
    {
        if (_nodeQuery.Resolve(host.Owner, ref host.Comp1, logMissing: false))
            yield return (host.Owner, host.Comp1);

        if (_polyQuery.Resolve(host.Owner, ref host.Comp2, logMissing: false))
        {
            foreach (var proxyId in host.Comp2.ProxyNodes.Values)
            {
                if (proxyId == host.Owner)
                    continue; // Already handled above.

                if (_nodeQuery.TryGetComponent(proxyId, out var proxy))
                    yield return (proxyId, proxy);
            }
        }
    }

    /// <summary>Enumerates all of the graphs associated with an entity.</summary>
    public IEnumerable<Entity<NodeGraphComponent>> EnumerateGraphs(Entity<GraphNodeComponent?, PolyNodeComponent?> host)
    {
        foreach (var node in EnumerateNodes(host))
        {
            if (_graphQuery.TryGetComponent(node.Comp.GraphId, out var graph))
                yield return (node.Comp.GraphId.Value, graph);
        }
    }


    /// <summary>
    /// Enumerates all of the nodes anchored to a tile.
    /// </summary>
    public IEnumerable<Entity<GraphNodeComponent>> GetAnchoredNodesOnTile(Entity<MapGridComponent> grid, Vector2i tileIndices)
    {
        foreach (var entityUid in _mapSys.GetAnchoredEntities(grid.Owner, grid.Comp, tileIndices))
        {
            foreach (var node in EnumerateNodes(entityUid))
                yield return node;
        }
    }

    /// <summary>
    /// Enumerates all of the nodes anchored to a tile in a direction.
    /// </summary>
    public IEnumerable<Entity<GraphNodeComponent>> GetAnchoredNodesInDir(Entity<MapGridComponent> grid, Vector2i tileIndices, Direction dir)
    {
        foreach (var node in GetAnchoredNodesOnTile(grid, (dir == Direction.Invalid) ? tileIndices : tileIndices.Offset(dir)))
            yield return node;
    }

    /// <summary>
    /// Enumerates all of the nodes anchored to tiles in directions.
    /// </summary>
    public IEnumerable<(Entity<GraphNodeComponent>, Direction)> GetAnchoredNodesInDirs<T>(Entity<MapGridComponent> grid, Vector2i tileIndices, T dirs)
        where T : IEnumerable<Direction>
    {
        foreach (var dir in dirs)
        {
            foreach (var node in GetAnchoredNodesInDir(grid, tileIndices, dir))
                yield return (node, dir);
        }
    }

    /// <inheritdoc cref="GetAnchoredNodesInDirs" />
    public IEnumerable<(Entity<GraphNodeComponent>, Direction)> GetAnchoredNodesInDirs(Entity<MapGridComponent> grid, Vector2i tileIndices, params Direction[] dirs)
    {
        return GetAnchoredNodesInDirs<Direction[]>(grid, tileIndices, dirs);
    }
}
