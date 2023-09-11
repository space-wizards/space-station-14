using Content.Shared.Nodes.Components;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared.Nodes.EntitySystems;

public abstract partial class SharedNodeGraphSystem
{
    protected void AttachProxy(EntityUid polyId, string proxyKey, EntityUid proxyId, PolyNodeComponent? poly = null, ProxyNodeComponent? proxy = null)
    {
        if (!Resolve(polyId, ref poly))
            return;

        if (poly.ProxyNodes.ContainsKey(proxyKey))
            return;

        if (!Resolve(proxyId, ref proxy))
            proxy = AddComp<ProxyNodeComponent>(proxyId);

        proxy.ProxyFor = polyId;
        proxy.ProxyKey = proxyKey;
        poly.ProxyNodes.Add(proxyKey, proxyId);
    }

    protected void DetachProxy(EntityUid polyId, string proxyKey, PolyNodeComponent? poly = null)
    {
        if (!Resolve(polyId, ref poly))
            return;

        if (!poly.ProxyNodes.Remove(proxyKey, out var proxyId))
            return;
        if (ProxyQuery.TryGetComponent(proxyId, out var proxy))
            proxy.ProxyFor = null;
    }

    protected void SpawnProxyNode(EntityUid polyId, string proxyKey, string? proxyPrototype, PolyNodeComponent poly)
    {
        var proxyId = EntityManager.CreateEntityUninitialized(proxyPrototype, new EntityCoordinates(polyId, Vector2.Zero), ProxyRegistry);
        var proxy = ProxyQuery.GetComponent(proxyId);

        AttachProxy(polyId, proxyKey, proxyId, poly, proxy);

        EntityManager.InitializeAndStartEntity(proxyId);
    }

    protected void OnComponentStartup(EntityUid uid, PolyNodeComponent comp, ComponentStartup args)
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

    protected void OnComponentShutdown(EntityUid uid, PolyNodeComponent comp, ComponentShutdown args)
    {
        while (comp.ProxyNodes.FirstOrNull() is { } proxy)
        {
            var (proxyKey, proxyId) = proxy;
            DetachProxy(uid, proxyKey, comp);
        }
    }

    protected void OnComponentShutdown(EntityUid uid, ProxyNodeComponent comp, ComponentShutdown args)
    {
        if (comp.ProxyFor is not { } polyId)
            return;

        if (PolyQuery.TryGetComponent(polyId, out var poly))
            DetachProxy(polyId, comp.ProxyKey, poly);
    }
}
