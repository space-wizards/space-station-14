using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>
    /// Attaches a given node to a polynode as a proxy node under a given key.
    /// </summary>
    private void AttachProxy(Entity<PolyNodeComponent?> host, Entity<ProxyNodeComponent?> proxy, string proxyKey)
    {
        if (!_polyQuery.Resolve(host.Owner, ref host.Comp))
            return;

        if (host.Comp.ProxyNodes.ContainsKey(proxyKey))
            return;

        if (!_proxyQuery.Resolve(proxy.Owner, ref proxy.Comp, logMissing: false))
            proxy.Comp = AddComp<ProxyNodeComponent>(proxy);

        host.Comp.ProxyNodes.Add(proxyKey, proxy);
        Dirty(host.Owner, host.Comp);

        proxy.Comp.ProxyFor = host.Owner;
        proxy.Comp.ProxyKey = proxyKey;
        Dirty(proxy.Owner, proxy.Comp);
    }

    /// <summary>
    /// Detaches a proxy node from a polynode.
    /// </summary>
    private void DetachProxy(Entity<PolyNodeComponent?> host, string proxyKey)
    {
        if (!_polyQuery.Resolve(host.Owner, ref host.Comp))
            return;

        if (!host.Comp.ProxyNodes.Remove(proxyKey, out var proxyId))
            return;

        Dirty(host.Owner, host.Comp);

        if (!_proxyQuery.TryGetComponent(proxyId, out var proxy))
            return;

        proxy.ProxyFor = null;
        proxy.ProxyKey = default!;
        Dirty(proxyId, proxy);
    }

    /// <summary>
    /// Creates a new proxy node for a polynode from scratch.
    /// </summary>
    private void SpawnProxyNode(Entity<PolyNodeComponent> host, string proxyKey, string? proxyPrototype)
    {
        var proxyId = EntityManager.CreateEntityUninitialized(proxyPrototype, new EntityCoordinates(host, Vector2.Zero));
        var proxy = EnsureComp<ProxyNodeComponent>(proxyId);

        AttachProxy((host.Owner, host.Comp), (proxyId, proxy), proxyKey);

        EntityManager.InitializeAndStartEntity(proxyId);
    }


    /// <summary>
    /// Generates and/or attaches 
    /// </summary>
    private void OnComponentStartup(EntityUid uid, PolyNodeComponent comp, ComponentStartup args)
    {
        if (comp.ProxySelf is { } selfKey)
            AttachProxy((uid, comp), uid, selfKey);

        if (comp.ProxyPrototypes is null)
            return;

        foreach (var (proxyKey, proxyProto) in comp.ProxyPrototypes)
        {
            if (comp.ProxyNodes.ContainsKey(proxyKey))
                continue;

            SpawnProxyNode((uid, comp), proxyKey, proxyProto);
        }
    }

    private void OnMapInit(EntityUid uid, PolyNodeComponent comp, MapInitEvent args)
    {
        foreach (var (_, proxyId) in comp.ProxyNodes)
        {
            EntityManager.RunMapInit(proxyId, MetaData(proxyId));
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

            DetachProxy((uid, comp), proxyKey);

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
            DetachProxy((polyId, poly), comp.ProxyKey!);
    }


    /// <summary>Relays a by-value event raised on a proxy node to its host polynode.</summary>
    /// <remarks>Exists so that the host node can react to graph/edge changes on its proxy nodes.</remarks>
    private void RelayProxyNodeValEvent<TEvent>(EntityUid uid, ProxyNodeComponent comp, TEvent args)
    {
        if (comp.ProxyFor is not { } proxyFor || proxyFor == uid)
            return;

        var hostEv = new ProxyNodeRelayEvent<TEvent>((uid, comp), args);
        RaiseLocalEvent(proxyFor, ref hostEv);
    }

    /// <summary>Relays a by-ref event raised on a proxy node to its host polynode.</summary>
    /// <remarks>Exists so that the host node can react to graph/edge changes on its proxy nodes.</remarks>
    private void RelayProxyNodeRefEvent<TEvent>(EntityUid uid, ProxyNodeComponent comp, ref TEvent args)
    {
        if (comp.ProxyFor is not { } proxyFor || proxyFor == uid)
            return;

        var hostEv = new ProxyNodeRelayEvent<TEvent>((uid, comp), args);
        RaiseLocalEvent(proxyFor, ref hostEv);
        args = hostEv.Event;
    }

    /// <summary>Relays a by-value event raised on a polynode to its hosted proxy nodes.</summary>
    /// <remarks>Exists so that proxy nodes can react to the movement/anchoring/unanchoring of its host polynode.</remarks>
    private void RelayPolyNodeValEvent<TEvent>(EntityUid uid, PolyNodeComponent comp, TEvent args)
    {
        if (comp.ProxyNodes.Count <= 0)
            return;

        var proxyEv = new PolyNodeRelayEvent<TEvent>((uid, comp), args);
        foreach (var proxyId in comp.ProxyNodes.Values)
        {
            if (proxyId == uid)
                continue;

            RaiseLocalEvent(proxyId, ref proxyEv);
        }
    }

    /// <summary>Relays a by-ref event raised on a polynode to its hosted proxy nodes.</summary>
    /// <remarks>Exists so that proxy nodes can react to the movement/anchoring/unanchoring of its host polynode.</remarks>
    private void RelayPolyNodeRefEvent<TEvent>(EntityUid uid, PolyNodeComponent comp, ref TEvent args)
    {
        if (comp.ProxyNodes.Count <= 0)
            return;

        var proxyEv = new PolyNodeRelayEvent<TEvent>((uid, comp), args);
        foreach (var proxyId in comp.ProxyNodes.Values)
        {
            if (proxyId == uid)
                continue;

            RaiseLocalEvent(proxyId, ref proxyEv);
        }

        args = proxyEv.Event;
    }
}
