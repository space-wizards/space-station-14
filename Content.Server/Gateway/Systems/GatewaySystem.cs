using Content.Server.Gateway.Components;
using Content.Shared.Gateway;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Gateway.Systems;

public sealed class GatewaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LinkedEntitySystem _linkedEntity = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatewayComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GatewayComponent, GatewayOpenPortalMessage>(OnOpenPortal);

        SubscribeLocalEvent<GatewayDestinationComponent, ComponentStartup>(OnDestinationStartup);
        SubscribeLocalEvent<GatewayDestinationComponent, ComponentShutdown>(OnDestinationShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // close portals after theyve been open long enough
        var query = EntityQueryEnumerator<GatewayComponent, PortalComponent>();
        while (query.MoveNext(out var uid, out var comp, out var _))
        {
            if (_timing.CurTime < comp.NextClose)
                continue;

            ClosePortal(uid, comp);
        }
    }

    private void UpdateUserInterface<T>(EntityUid uid, GatewayComponent comp, T args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void UpdateUserInterface(EntityUid uid, GatewayComponent comp)
    {
        var destinations = new List<(EntityUid, String, TimeSpan, bool)>();
        foreach (var destUid in comp.Destinations)
        {
            var dest = Comp<GatewayDestinationComponent>(destUid);
            if (!dest.Enabled)
                continue;

            destinations.Add((destUid, dest.Name, dest.NextReady, HasComp<PortalComponent>(destUid)));
        }

        GetDestination(uid, out var current);
        var state = new GatewayBoundUserInterfaceState(destinations, current);
        _ui.TrySetUiState(uid, GatewayUiKey.Key, state);
    }

    private void OnOpenPortal(EntityUid uid, GatewayComponent comp, GatewayOpenPortalMessage args)
    {
        // can't link if portal is already open, the destination is invalid or on cooldown
        if (HasComp<PortalComponent>(uid) ||
            !TryComp<GatewayDestinationComponent>(args.Destination, out var dest) ||
            !dest.Enabled ||
            _timing.CurTime < dest.NextReady)
            return;

        // TODO: admin log???
        OpenPortal(uid, comp, args.Destination, dest);
    }

    private void OpenPortal(EntityUid uid, GatewayComponent comp, EntityUid dest, GatewayDestinationComponent destComp)
    {
        _linkedEntity.TryLink(uid, dest);
        EnsureComp<PortalComponent>(uid);
        EnsureComp<PortalComponent>(dest);

        // close automatically after time is up
        comp.NextClose = _timing.CurTime + destComp.OpenTime;

        // prevent destination being linked by another gateway
        destComp.Enabled = false;
    }

    private void ClosePortal(EntityUid uid, GatewayComponent comp)
    {
        RemComp<PortalComponent>(uid);
        if (!GetDestination(uid, out var destUid))
            return;

        if (TryComp<GatewayDestinationComponent>(destUid, out var dest))
        {
            // portals closed, show it in the ui again
            dest.NextReady = _timing.CurTime + dest.Cooldown;
            dest.Enabled = true;
        }

        _linkedEntity.TryUnlink(uid, destUid.Value);
        RemComp<PortalComponent>(destUid.Value);
    }

    private bool GetDestination(EntityUid uid, [NotNullWhen(true)] out EntityUid? dest)
    {
        dest = null;
        if (TryComp<LinkedEntityComponent>(uid, out var linked))
        {
            var first = linked.LinkedEntities.FirstOrDefault();
            if (first != EntityUid.Invalid)
            {
                dest = first;
                return true;
            }
        }

        return false;
    }

    private void OnDestinationStartup(EntityUid uid, GatewayDestinationComponent comp, ComponentStartup args)
    {
        var query = EntityQueryEnumerator<GatewayComponent>();
        while (query.MoveNext(out var gatewayUid, out var gateway))
        {
            gateway.Destinations.Add(uid);
            UpdateUserInterface(gatewayUid, gateway);
        }
    }

    private void OnDestinationShutdown(EntityUid uid, GatewayDestinationComponent comp, ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<GatewayComponent>();
        while (query.MoveNext(out var gatewayUid, out var gateway))
        {
            gateway.Destinations.Remove(uid);
            UpdateUserInterface(gatewayUid, gateway);
        }
    }
}
