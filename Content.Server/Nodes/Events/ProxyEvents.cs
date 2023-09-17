using Content.Server.Nodes.Components;

namespace Content.Server.Nodes.Events;

/// <summary>The event used to relay events raised on poly nodes to their respective proxy nodes.</summary>
/// <remarks>Primarily used to ensure that proxy nodes update when the parent object is moved around or (un)anchored.</remarks>
[ByRefEvent]
public record struct PolyNodeRelayEvent<TEvent>(EntityUid PolyId, TEvent Event, PolyNodeComponent Poly)
{
    public readonly EntityUid PolyId = PolyId;
    public readonly PolyNodeComponent Poly = Poly;
    public TEvent Event = Event;
}

/// <summary>The event used to relay events raised on proxy nodes to their host polynode.</summary>
/// <remarks>Primarily used to ensure that poly nodes can react to the connectivity of their proxy nodes changing.</remarks>
[ByRefEvent]
public record struct ProxyNodeRelayEvent<TEvent>(EntityUid ProxyId, TEvent Event, ProxyNodeComponent Proxy)
{
    public readonly EntityUid ProxyId = ProxyId;
    public readonly ProxyNodeComponent Proxy = Proxy;
    public TEvent Event = Event;
}
