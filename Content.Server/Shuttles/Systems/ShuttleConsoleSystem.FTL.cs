using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private void InitializeFTL()
    {
        SubscribeLocalEvent<FTLBeaconComponent, ComponentStartup>(OnBeaconStartup);
        SubscribeLocalEvent<FTLBeaconComponent, AnchorStateChangedEvent>(OnBeaconAnchorChanged);

        SubscribeLocalEvent<FTLExclusionComponent, ComponentStartup>(OnExclusionStartup);
    }

    private void OnExclusionStartup(Entity<FTLExclusionComponent> ent, ref ComponentStartup args)
    {
        RefreshShuttleConsoles();
    }

    private void OnBeaconStartup(Entity<FTLBeaconComponent> ent, ref ComponentStartup args)
    {
        RefreshShuttleConsoles();
    }

    private void OnBeaconAnchorChanged(Entity<FTLBeaconComponent> ent, ref AnchorStateChangedEvent args)
    {
        RefreshShuttleConsoles();
    }

    private void OnBeaconFTLMessage(Entity<ShuttleConsoleComponent> ent, ref ShuttleConsoleFTLBeaconMessage args)
    {
        var beaconEnt = GetEntity(args.Beacon);
        if (!_xformQuery.TryGetComponent(beaconEnt, out var targetXform))
        {
            return;
        }

        var nCoordinates = new NetCoordinates(GetNetEntity(targetXform.ParentUid), targetXform.LocalPosition);
        if (targetXform.ParentUid == EntityUid.Invalid)
        {
            nCoordinates = new NetCoordinates(GetNetEntity(beaconEnt), targetXform.LocalPosition);
        }

        // Check target exists
        if (!_shuttle.CanFTLBeacon(nCoordinates))
        {
            return;
        }

        var angle = args.Angle.Reduced();
        var targetCoordinates = new EntityCoordinates(targetXform.MapUid!.Value, _transform.GetWorldPosition(targetXform));

        if (TryComp(beaconEnt, out FTLDockingBeaconComponent? dockingBeacon))
        {
            var beaconCoordinates = targetXform.Coordinates;

            if (TryGetDockingBeaconTarget(dockingBeacon, out var dockingTarget, out var dockingMap) &&
                TryConsoleDockFTL(ent, dockingTarget, beaconCoordinates, angle, dockingMap, dockingBeacon))
            {
                return;
            }

            ConsoleBeaconFallbackFTL(ent, beaconCoordinates, angle, targetXform.MapID, dockingBeacon);
            return;
        }

        ConsoleFTL(ent, targetCoordinates, angle, targetXform.MapID);
    }

    private bool TryGetDockingBeaconTarget(
        FTLDockingBeaconComponent beacon,
        out EntityUid target,
        out MapId targetMap)
    {
        target = default;
        targetMap = MapId.Nullspace;

        if (beacon.TargetGrid == null ||
            !Exists(beacon.TargetGrid.Value) ||
            !_xformQuery.TryGetComponent(beacon.TargetGrid.Value, out var targetXform) ||
            targetXform.MapID == MapId.Nullspace)
        {
            return false;
        }

        target = beacon.TargetGrid.Value;
        targetMap = targetXform.MapID;
        return true;
    }

    private void OnPositionFTLMessage(Entity<ShuttleConsoleComponent> entity, ref ShuttleConsoleFTLPositionMessage args)
    {
        var mapUid = _mapSystem.GetMap(args.Coordinates.MapId);

        // If it's beacons only block all position messages.
        if (!Exists(mapUid) || _shuttle.IsBeaconMap(mapUid))
        {
            return;
        }

        var targetCoordinates = new EntityCoordinates(mapUid, args.Coordinates.Position);
        var angle = args.Angle.Reduced();
        ConsoleFTL(entity, targetCoordinates, angle, args.Coordinates.MapId);
    }

    private void GetBeacons(ref List<ShuttleBeaconObject>? beacons)
    {
        var beaconQuery = AllEntityQuery<FTLBeaconComponent>();

        while (beaconQuery.MoveNext(out var destUid, out _))
        {
            var meta = _metaQuery.GetComponent(destUid);
            var name = meta.EntityName;

            if (string.IsNullOrEmpty(name))
                name = Loc.GetString("shuttle-console-unknown");

            // Can't travel to same map (yet)
            var destXform = _xformQuery.GetComponent(destUid);
            beacons ??= new List<ShuttleBeaconObject>();
            beacons.Add(new ShuttleBeaconObject(GetNetEntity(destUid), GetNetCoordinates(destXform.Coordinates), name));
        }
    }

    private void GetExclusions(ref List<ShuttleExclusionObject>? exclusions)
    {
        var query = AllEntityQuery<FTLExclusionComponent, TransformComponent>();

        while (query.MoveNext(out var comp, out var xform))
        {
            if (!comp.Enabled)
                continue;

            exclusions ??= new List<ShuttleExclusionObject>();
            exclusions.Add(new ShuttleExclusionObject(GetNetCoordinates(xform.Coordinates), comp.Range, Loc.GetString("shuttle-console-exclusion")));
        }
    }

    /// <summary>
    /// Handles shuttle console FTLs.
    /// </summary>
    private void ConsoleFTL(Entity<ShuttleConsoleComponent> ent, EntityCoordinates targetCoordinates, Angle targetAngle, MapId targetMap)
    {
        var consoleUid = GetDroneConsole(ent.Owner);

        if (consoleUid == null)
            return;

        var shuttleUid = _xformQuery.GetComponent(consoleUid.Value).GridUid;

        if (!TryComp(shuttleUid, out ShuttleComponent? shuttleComp))
            return;

        if (shuttleComp.Enabled == false)
            return;

        // Check shuttle can even FTL
        if (!_shuttle.CanFTL(shuttleUid.Value, out var reason))
        {
            // TODO: Session popup
            return;
        }

        // Check shuttle can FTL to this target.
        if (!CanConsoleFTLToMap(shuttleUid.Value, targetMap, ent))
        {
            return;
        }

        List<ShuttleExclusionObject>? exclusions = null;
        GetExclusions(ref exclusions);

        if (!_shuttle.FTLFree(shuttleUid.Value, targetCoordinates, targetAngle, exclusions))
        {
            return;
        }

        if (!TryComp(shuttleUid.Value, out PhysicsComponent? shuttlePhysics))
        {
            return;
        }

        // Client sends the "adjusted" coordinates and we adjust it back to get the actual transform coordinates.
        var adjustedCoordinates = targetCoordinates.Offset(targetAngle.RotateVec(-shuttlePhysics.LocalCenter));

        var tagEv = new FTLTagEvent();
        RaiseLocalEvent(shuttleUid.Value, ref tagEv);

        var ev = new ShuttleConsoleFTLTravelStartEvent(ent.Owner);
        RaiseLocalEvent(ref ev);

        _shuttle.FTLToCoordinates(shuttleUid.Value, shuttleComp, adjustedCoordinates, targetAngle);
    }

    private void ConsoleBeaconFallbackFTL(
        Entity<ShuttleConsoleComponent> ent,
        EntityCoordinates targetCoordinates,
        Angle targetAngle,
        MapId targetMap,
        FTLDockingBeaconComponent dockingBeacon)
    {
        var consoleUid = GetDroneConsole(ent.Owner);

        if (consoleUid == null)
            return;

        var shuttleUid = _xformQuery.GetComponent(consoleUid.Value).GridUid;

        if (!TryComp(shuttleUid, out ShuttleComponent? shuttleComp))
            return;

        if (shuttleComp.Enabled == false)
            return;

        if (!_shuttle.CanFTL(shuttleUid.Value, out _))
            return;

        if (!CanConsoleFTLToMap(shuttleUid.Value, targetMap, ent))
            return;

        var tagEv = new FTLTagEvent();
        RaiseLocalEvent(shuttleUid.Value, ref tagEv);

        var ev = new ShuttleConsoleFTLTravelStartEvent(ent.Owner);
        RaiseLocalEvent(ref ev);

        _shuttle.FTLToCoordinates(
            shuttleUid.Value,
            shuttleComp,
            targetCoordinates,
            targetAngle,
            useProximity: true,
            proximityMinOffset: dockingBeacon.FallbackMinOffset,
            proximityMaxOffset: dockingBeacon.FallbackMaxOffset);
    }

    private bool TryConsoleDockFTL(
        Entity<ShuttleConsoleComponent> ent,
        EntityUid dockingTarget,
        EntityCoordinates fallbackCoordinates,
        Angle targetAngle,
        MapId targetMap,
        FTLDockingBeaconComponent dockingBeacon)
    {
        var consoleUid = GetDroneConsole(ent.Owner);

        if (consoleUid == null)
            return false;

        var shuttleUid = _xformQuery.GetComponent(consoleUid.Value).GridUid;

        if (!TryComp(shuttleUid, out ShuttleComponent? shuttleComp))
            return false;

        if (shuttleComp.Enabled == false)
            return false;

        if (_whitelist.IsWhitelistFail(dockingBeacon.DockWhitelist, shuttleUid.Value))
            return false;

        if (!_shuttle.CanFTL(shuttleUid.Value, out _))
            return false;

        if (!CanConsoleFTLToMap(shuttleUid.Value, targetMap, ent))
            return false;

        var ev = new ShuttleConsoleFTLTravelStartEvent(ent.Owner);
        RaiseLocalEvent(ref ev);

        _shuttle.FTLToDock(
            shuttleUid.Value,
            shuttleComp,
            dockingTarget,
            dockingBeacon.StartupTime,
            dockingBeacon.HyperspaceTime,
            dockingBeacon.PriorityTag,
            fallbackCoordinates,
            targetAngle,
            dockingBeacon.FallbackMinOffset,
            dockingBeacon.FallbackMaxOffset);
        return true;
    }

    private bool CanConsoleFTLToMap(EntityUid shuttleUid, MapId targetMap, EntityUid consoleUid)
    {
        if (IsShuttleAlreadyOnPlanetMap(shuttleUid, targetMap))
            return false;

        return _shuttle.CanFTLTo(shuttleUid, targetMap, consoleUid);
    }

    private bool IsShuttleAlreadyOnPlanetMap(EntityUid shuttleUid, MapId targetMap)
    {
        if (!_xformQuery.TryGetComponent(shuttleUid, out var shuttleXform) ||
            shuttleXform.MapID != targetMap)
        {
            return false;
        }

        var mapUid = _mapSystem.GetMapOrInvalid(targetMap);
        return mapUid.IsValid() &&
               (HasComp<LavalandMapComponent>(mapUid) || HasComp<PrisonMapComponent>(mapUid));
    }

    private void RemoveCurrentPlanetBeacons(EntityUid shuttleUid, List<ShuttleBeaconObject>? beacons)
    {
        if (beacons == null ||
            !_xformQuery.TryGetComponent(shuttleUid, out var shuttleXform) ||
            !IsShuttleAlreadyOnPlanetMap(shuttleUid, shuttleXform.MapID))
        {
            return;
        }

        for (var i = beacons.Count - 1; i >= 0; i--)
        {
            var beaconCoordinates = _transform.ToMapCoordinates(GetCoordinates(beacons[i].Coordinates));
            if (beaconCoordinates.MapId == shuttleXform.MapID)
                beacons.RemoveAt(i);
        }
    }
}
