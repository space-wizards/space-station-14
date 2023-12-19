using Content.Server.Gateway.Components;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
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
using Robust.Shared.Utility;

namespace Content.Server.Gateway.Systems;

public sealed class GatewaySystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LinkedEntitySystem _linkedEntity = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatewayComponent, EntityUnpausedEvent>(OnGatewayUnpaused);
        SubscribeLocalEvent<GatewayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GatewayComponent, ActivatableUIOpenAttemptEvent>(OnGatewayOpenAttempt);
        SubscribeLocalEvent<GatewayComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GatewayComponent, GatewayOpenPortalMessage>(OnOpenPortal);
    }

    public void SetEnabled(EntityUid uid, bool value, GatewayComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Enabled == value)
            return;

        component.Enabled = value;
        UpdateAllGateways();
    }

    private void OnGatewayUnpaused(EntityUid uid, GatewayComponent component, ref EntityUnpausedEvent args)
    {
        component.NextReady += args.PausedTime;
    }

    private void OnStartup(EntityUid uid, GatewayComponent comp, ComponentStartup args)
    {
        // no need to update ui since its just been created, just do portal
        UpdateAppearance(uid);
    }

    private void OnGatewayOpenAttempt(EntityUid uid, GatewayComponent component, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!component.Enabled || !component.Interactable)
            args.Cancel();
    }

    private void UpdateUserInterface<T>(EntityUid uid, GatewayComponent comp, T args)
    {
        UpdateUserInterface(uid, comp);
    }

    public void UpdateAllGateways()
    {
        var query = AllEntityQuery<GatewayComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdateUserInterface(uid, comp, xform);
        }
    }

    private void UpdateUserInterface(EntityUid uid, GatewayComponent comp, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        var destinations = new List<GatewayDestinationData>();
        var query = AllEntityQuery<GatewayComponent, TransformComponent>();

        var nextUnlock = TimeSpan.Zero;
        var unlockTime = TimeSpan.Zero;

        // Next unlock is based off of:
        // - Our station's unlock timer (if we have a station)
        // - If our map is a generated destination then use the generator that made it

        if (TryComp(_stations.GetOwningStation(uid), out GatewayGeneratorComponent? generatorComp) ||
            (TryComp(xform.MapUid, out GatewayGeneratorDestinationComponent? generatorDestination) &&
             TryComp(generatorDestination.Generator, out generatorComp)))
        {
            nextUnlock = generatorComp.NextUnlock;
            unlockTime = generatorComp.UnlockCooldown;
        }

        while (query.MoveNext(out var destUid, out var dest, out var destXform))
        {
            if (!dest.Enabled || destUid == uid)
                continue;

            // Show destination if either no destination comp on the map or it's ours.
            TryComp<GatewayGeneratorDestinationComponent>(destXform.MapUid, out var gatewayDestination);

            destinations.Add(new GatewayDestinationData()
            {
                Entity = GetNetEntity(destUid),
                // Fallback to grid's ID if applicable.
                Name = dest.Name.IsEmpty && destXform.GridUid != null ? FormattedMessage.FromUnformatted(MetaData(destXform.GridUid.Value).EntityName) : dest.Name ,
                Portal = HasComp<PortalComponent>(destUid),
                // If NextUnlock < CurTime it's unlocked, however
                // we'll always send the client if it's locked
                // It can just infer unlock times locally and not have to worry about it here.
                Locked = gatewayDestination != null && gatewayDestination.Locked
            });
        }

        _linkedEntity.GetLink(uid, out var current);

        var state = new GatewayBoundUserInterfaceState(
            destinations,
            GetNetEntity(current),
            comp.NextReady,
            comp.Cooldown,
            nextUnlock,
            unlockTime
        );

        _ui.TrySetUiState(uid, GatewayUiKey.Key, state);
    }

    private void UpdateAppearance(EntityUid uid)
    {
        _appearance.SetData(uid, GatewayVisuals.Active, HasComp<PortalComponent>(uid));
    }

    private void OnOpenPortal(EntityUid uid, GatewayComponent comp, GatewayOpenPortalMessage args)
    {
        if (args.Session.AttachedEntity == null || GetNetEntity(uid) == args.Destination ||
            !comp.Enabled || !comp.Interactable)
            return;

        // if the gateway has an access reader check it before allowing opening
        var user = args.Session.AttachedEntity.Value;
        if (CheckAccess(user, uid, comp))
            return;

        // can't link if portal is already open on either side, the destination is invalid or on cooldown
        var desto = GetEntity(args.Destination);

        // If it's already open / not enabled / we're not ready DENY.
        if (!TryComp<GatewayComponent>(desto, out var dest) ||
            !dest.Enabled ||
            _timing.CurTime < _metadata.GetPauseTime(uid) + comp.NextReady)
        {
            return;
        }

        // TODO: admin log???
        ClosePortal(uid, comp, false);
        OpenPortal(uid, comp, desto, dest);
    }

    private void OpenPortal(EntityUid uid, GatewayComponent comp, EntityUid dest, GatewayComponent destComp, TransformComponent? destXform = null)
    {
        if (!Resolve(dest, ref destXform) || destXform.MapUid == null)
            return;

        var ev = new AttemptGatewayOpenEvent(destXform.MapUid.Value, dest);
        RaiseLocalEvent(destXform.MapUid.Value, ref ev);

        if (ev.Cancelled)
            return;

        _linkedEntity.OneWayLink(uid, dest);

        var sourcePortal = EnsureComp<PortalComponent>(uid);
        var targetPortal = EnsureComp<PortalComponent>(dest);

        sourcePortal.CanTeleportToOtherMaps = true;
        targetPortal.CanTeleportToOtherMaps = true;

        sourcePortal.RandomTeleport = false;
        targetPortal.RandomTeleport = false;

        var openEv = new GatewayOpenEvent(destXform.MapUid.Value, dest);
        RaiseLocalEvent(destXform.MapUid.Value, ref openEv);

        // for ui
        comp.NextReady = _timing.CurTime + comp.Cooldown;

        _audio.PlayPvs(comp.OpenSound, uid);
        _audio.PlayPvs(comp.OpenSound, dest);

        UpdateUserInterface(uid, comp);
        UpdateAppearance(uid);
        UpdateAppearance(dest);
    }

    private void ClosePortal(EntityUid uid, GatewayComponent? comp = null, bool update = true)
    {
        if (!Resolve(uid, ref comp))
            return;

        RemComp<PortalComponent>(uid);
        if (!_linkedEntity.GetLink(uid, out var dest))
            return;

        if (TryComp<GatewayComponent>(dest, out var destComp))
        {
            // portals closed, put it on cooldown and let it eventually be opened again
            destComp.NextReady = _timing.CurTime + destComp.Cooldown;
        }

        _audio.PlayPvs(comp.CloseSound, uid);
        _audio.PlayPvs(comp.CloseSound, dest.Value);

        _linkedEntity.TryUnlink(uid, dest.Value);
        RemComp<PortalComponent>(dest.Value);

        if (update)
        {
            UpdateUserInterface(uid, comp);
            UpdateAppearance(uid);
            UpdateAppearance(dest.Value);
        }
    }

    private void OnDestinationStartup(EntityUid uid, GatewayComponent comp, ComponentStartup args)
    {
        var query = AllEntityQuery<GatewayComponent>();
        while (query.MoveNext(out var gatewayUid, out var gateway))
        {
            UpdateUserInterface(gatewayUid, gateway);
        }

        UpdateAppearance(uid);
    }

    private void OnDestinationShutdown(EntityUid uid, GatewayComponent comp, ComponentShutdown args)
    {
        var query = AllEntityQuery<GatewayComponent>();
        while (query.MoveNext(out var gatewayUid, out var gateway))
        {
            UpdateUserInterface(gatewayUid, gateway);
        }
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

    public void SetDestinationName(EntityUid gatewayUid, FormattedMessage gatewayName, GatewayComponent? gatewayComp = null)
    {
        if (!Resolve(gatewayUid, ref gatewayComp))
            return;

        gatewayComp.Name = gatewayName;
        Dirty(gatewayUid, gatewayComp);
    }
}

/// <summary>
/// Raised directed on the target map when a GatewayDestination is attempted to be opened.
/// </summary>
[ByRefEvent]
public record struct AttemptGatewayOpenEvent(EntityUid MapUid, EntityUid GatewayDestinationUid)
{
    public readonly EntityUid MapUid = MapUid;
    public readonly EntityUid GatewayDestinationUid = GatewayDestinationUid;

    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the target map when a gateway is opened.
/// </summary>
[ByRefEvent]
public readonly record struct GatewayOpenEvent(EntityUid MapUid, EntityUid GatewayDestinationUid);
