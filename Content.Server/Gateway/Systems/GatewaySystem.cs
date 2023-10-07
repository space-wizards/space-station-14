using Content.Server.Gateway.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Gateway;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Gateway.Systems;

public sealed class GatewaySystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LinkedEntitySystem _linkedEntity = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatewayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GatewayComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GatewayComponent, GatewayOpenPortalMessage>(OnOpenPortal);

        SubscribeLocalEvent<GatewayDestinationComponent, ComponentStartup>(OnDestinationStartup);
        SubscribeLocalEvent<GatewayDestinationComponent, ComponentShutdown>(OnDestinationShutdown);
        SubscribeLocalEvent<GatewayDestinationComponent, GetVerbsEvent<AlternativeVerb>>(OnDestinationGetVerbs);
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

    private void OnStartup(EntityUid uid, GatewayComponent comp, ComponentStartup args)
    {
        // add existing destinations
        var query = EntityQueryEnumerator<GatewayDestinationComponent>();
        while (query.MoveNext(out var dest, out _))
        {
            comp.Destinations.Add(dest);
        }

        // no need to update ui since its just been created, just do portal
        UpdateAppearance(uid);
    }

    private void UpdateUserInterface<T>(EntityUid uid, GatewayComponent comp, T args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void UpdateUserInterface(EntityUid uid, GatewayComponent comp)
    {
        var destinations = new List<(NetEntity, string, TimeSpan, bool)>();
        foreach (var destUid in comp.Destinations)
        {
            var dest = Comp<GatewayDestinationComponent>(destUid);
            if (!dest.Enabled)
                continue;

            destinations.Add((GetNetEntity(destUid), dest.Name, dest.NextReady, HasComp<PortalComponent>(destUid)));
        }

        _linkedEntity.GetLink(uid, out var current);
        var state = new GatewayBoundUserInterfaceState(destinations, GetNetEntity(current), comp.NextClose, comp.LastOpen);
        _ui.TrySetUiState(uid, GatewayUiKey.Key, state);
    }

    private void UpdateAppearance(EntityUid uid)
    {
        _appearance.SetData(uid, GatewayVisuals.Active, HasComp<PortalComponent>(uid));
    }

    private void OnOpenPortal(EntityUid uid, GatewayComponent comp, GatewayOpenPortalMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        // if the gateway has an access reader check it before allowing opening
        var user = args.Session.AttachedEntity.Value;
        if (CheckAccess(user, uid))
            return;

        // can't link if portal is already open on either side, the destination is invalid or on cooldown
        var desto = GetEntity(args.Destination);

        if (HasComp<PortalComponent>(uid) ||
            HasComp<PortalComponent>(desto) ||
            !TryComp<GatewayDestinationComponent>(desto, out var dest) ||
            !dest.Enabled ||
            _timing.CurTime < dest.NextReady)
            return;

        // TODO: admin log???
        OpenPortal(uid, comp, desto, dest);
    }

    private void OpenPortal(EntityUid uid, GatewayComponent comp, EntityUid dest, GatewayDestinationComponent destComp)
    {
        _linkedEntity.TryLink(uid, dest);
        EnsureComp<PortalComponent>(uid).CanTeleportToOtherMaps = true;
        EnsureComp<PortalComponent>(dest).CanTeleportToOtherMaps = true;

        // for ui
        comp.LastOpen = _timing.CurTime;
        // close automatically after time is up
        comp.NextClose = comp.LastOpen + destComp.OpenTime;

        _audio.PlayPvs(comp.OpenSound, uid);
        _audio.PlayPvs(comp.OpenSound, dest);

        UpdateUserInterface(uid, comp);
        UpdateAppearance(uid);
        UpdateAppearance(dest);
    }

    private void ClosePortal(EntityUid uid, GatewayComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        RemComp<PortalComponent>(uid);
        if (!_linkedEntity.GetLink(uid, out var dest))
            return;

        if (TryComp<GatewayDestinationComponent>(dest, out var destComp))
        {
            // portals closed, put it on cooldown and let it eventually be opened again
            destComp.NextReady = _timing.CurTime + destComp.Cooldown;
        }

        _audio.PlayPvs(comp.CloseSound, uid);
        _audio.PlayPvs(comp.CloseSound, dest.Value);

        _linkedEntity.TryUnlink(uid, dest.Value);
        RemComp<PortalComponent>(dest.Value);
        UpdateUserInterface(uid, comp);
        UpdateAppearance(uid);
        UpdateAppearance(dest.Value);
    }

    private void OnDestinationStartup(EntityUid uid, GatewayDestinationComponent comp, ComponentStartup args)
    {
        var query = EntityQueryEnumerator<GatewayComponent>();
        while (query.MoveNext(out var gatewayUid, out var gateway))
        {
            gateway.Destinations.Add(uid);
            UpdateUserInterface(gatewayUid, gateway);
        }

        UpdateAppearance(uid);
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

    private void OnDestinationGetVerbs(EntityUid uid, GatewayDestinationComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!comp.Closeable || !args.CanInteract || !args.CanAccess)
            return;

        // a portal is open so add verb to close it
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryClose(uid, args.User),
            Text = Loc.GetString("gateway-close-portal")
        });
    }

    private void TryClose(EntityUid uid, EntityUid user)
    {
        // portal already closed so cant close it
        if (!_linkedEntity.GetLink(uid, out var source))
            return;

        // not allowed to close it
        if (CheckAccess(user, source.Value))
            return;

        ClosePortal(source.Value);
    }

    /// <summary>
    /// Checks the user's access. Makes popup and plays sound if missing access.
    /// Returns whether access was missing.
    /// </summary>
    private bool CheckAccess(EntityUid user, EntityUid uid, GatewayComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (_accessReader.IsAllowed(user, uid))
            return false;

        _popup.PopupEntity(Loc.GetString("gateway-access-denied"), user);
        _audio.PlayPvs(comp.AccessDeniedSound, uid);
        return true;
    }
}
