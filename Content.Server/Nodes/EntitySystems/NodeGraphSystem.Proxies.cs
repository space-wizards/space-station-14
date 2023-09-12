using Content.Server.Nodes.Components;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Attaches a given node to a polynode as a proxy node under a given key.
    /// </summary>
    private void AttachProxy(EntityUid polyId, string proxyKey, EntityUid proxyId, PolyNodeComponent? poly = null, ProxyNodeComponent? proxy = null)
    {
        if (!_polyQuery.Resolve(polyId, ref poly))
            return;

        if (poly.ProxyNodes.ContainsKey(proxyKey))
            return;

        if (!_proxyQuery.Resolve(proxyId, ref proxy))
            proxy = AddComp<ProxyNodeComponent>(proxyId);

        proxy.ProxyFor = polyId;
        proxy.ProxyKey = proxyKey;
        poly.ProxyNodes.Add(proxyKey, proxyId);
        Dirty(proxyId, proxy);
        Dirty(polyId, poly);
    }

    /// <summary>
    /// Detaches a proxy node from a polynode.
    /// </summary>
    private void DetachProxy(EntityUid polyId, string proxyKey, PolyNodeComponent? poly = null)
    {
        if (!_polyQuery.Resolve(polyId, ref poly))
            return;

        if (!poly.ProxyNodes.Remove(proxyKey, out var proxyId))
            return;

        Dirty(polyId, poly);
        if (!_proxyQuery.TryGetComponent(proxyId, out var proxy))
            return;

        proxy.ProxyFor = null;
        Dirty(proxyId, proxy);
    }

    /// <summary>
    /// Creates a new proxy node for a polynode from scratch.
    /// </summary>
    private void SpawnProxyNode(EntityUid polyId, string proxyKey, string? proxyPrototype, PolyNodeComponent poly)
    {
        var proxyId = EntityManager.CreateEntityUninitialized(proxyPrototype, new EntityCoordinates(polyId, Vector2.Zero));
        var proxy = EnsureComp<ProxyNodeComponent>(proxyId);

        AttachProxy(polyId, proxyKey, proxyId, poly, proxy);

        EntityManager.InitializeAndStartEntity(proxyId);
    }


    /// <summary>
    /// Generates and/or attaches 
    /// </summary>
    private void OnComponentStartup(EntityUid uid, PolyNodeComponent comp, ComponentStartup args)
    {
        if (comp.ProxySelf is { } selfKey)
            AttachProxy(uid, selfKey, uid, comp);

        if (comp.ProxyPrototypes is null)
            return;

        foreach (var (proxyKey, proxyProto) in comp.ProxyPrototypes)
        {
            if (comp.ProxyNodes.ContainsKey(proxyKey))
                continue;

            SpawnProxyNode(uid, proxyKey, proxyProto, comp);
        }
    }

    /// <summary>
    /// Detaches and deletes any proxy nodes associated with polynodes that are becoming not such.
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, PolyNodeComponent comp, ComponentShutdown args)
    {
        while (comp.ProxyNodes.FirstOrNull() is { } proxy)
        {
            var (proxyKey, proxyId) = proxy;

            DetachProxy(uid, proxyKey, comp);

            if (proxyId != uid)
                QueueDel(proxyId);
        }
    }

    /// <summary>
    /// Detaches any proxynodes that are becoming not such from their host polynode.
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, ProxyNodeComponent comp, ComponentShutdown args)
    {
        if (comp.ProxyFor is not { } polyId)
            return;

        if (_polyQuery.TryGetComponent(polyId, out var poly))
            DetachProxy(polyId, comp.ProxyKey!, poly);
    }
}
