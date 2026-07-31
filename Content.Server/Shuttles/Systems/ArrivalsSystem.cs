using System.Numerics;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tiles;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Spawns latejoin players directly on the arrivals shuttle and sends it from a hidden holding map to the station.
/// </summary>
public sealed class ArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private EntityQuery<PendingClockInComponent> _pendingQuery;

    private static readonly TimeSpan DepartureDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan EmptyReturnDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If enabled then latejoin players arrive by shuttle.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    /// Flags if latejoin arrivals have godmode until they leave the shuttle.
    /// </summary>
    public bool ArrivalsGodmode { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: new []{ typeof(SpawnPointSystem)}, after: new [] { typeof(ContainerSpawnPointSystem)});

        SubscribeLocalEvent<StationArrivalsComponent, StationPostInitEvent>(OnStationPostInit);

        SubscribeLocalEvent<ArrivalsShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLStartedEvent>(OnArrivalsFTL);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLCompletedEvent>(OnArrivalsDocked);
        SubscribeLocalEvent<DockingComponent, UndockEvent>(OnDockUndocked);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(SendDirections);

        _pendingQuery = GetEntityQuery<PendingClockInComponent>();

        // Don't invoke immediately as it will get set in the natural course of things.
        Enabled = _cfgManager.GetCVar(CCVars.ArrivalsShuttles);
        ArrivalsGodmode = _cfgManager.GetCVar(CCVars.GodmodeArrivals);

        _cfgManager.OnValueChanged(CCVars.ArrivalsShuttles, SetArrivals);
        _cfgManager.OnValueChanged(CCVars.GodmodeArrivals, b => ArrivalsGodmode = b);

        // Command so admins can set these for funsies.
        _console.RegisterCommand("arrivals", ArrivalsCommand, ArrivalsCompletion);
    }

    private void OnShuttleTag(EntityUid uid, ArrivalsShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting. (v2 boogaloo)
        args.Handled = true;
        args.Tag = "DockArrivals";
    }

    private CompletionResult ArrivalsCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return new CompletionResult(new CompletionOption[]
        {
            // Enables and disable are separate comms in case you don't want to accidentally toggle it, compared to
            // returns which doesn't have an immediate effect
            new("enable", Loc.GetString("cmd-arrivals-enable-hint")),
            new("disable", Loc.GetString("cmd-arrivals-disable-hint")),
            new("returns", Loc.GetString("cmd-arrivals-returns-hint")),
            new ("force", Loc.GetString("cmd-arrivals-force-hint"))
        }, "Option");
    }

    [AdminCommand(AdminFlags.Fun)]
    private void ArrivalsCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-arrivals-invalid"));
            return;
        }

        switch (args[0])
        {
            case "enable":
                _cfgManager.SetCVar(CCVars.ArrivalsShuttles, true);
                break;
            case "disable":
                _cfgManager.SetCVar(CCVars.ArrivalsShuttles, false);
                break;
            case "returns":
                var existing = _cfgManager.GetCVar(CCVars.ArrivalsReturns);
                _cfgManager.SetCVar(CCVars.ArrivalsReturns, !existing);
                shell.WriteLine(Loc.GetString("cmd-arrivals-returns", ("value", !existing)));
                break;
            case "force":
                ForcePendingArrivals(shell);
                break;
            default:
                shell.WriteError(Loc.GetString($"cmd-arrivals-invalid"));
                break;
        }
    }

    private void ForcePendingArrivals(IConsoleShell shell)
    {
        var query = AllEntityQuery<PendingClockInComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var pendingXform))
        {
            if (!TryGetArrivalsShuttle(pendingXform.GridUid, out var shuttle))
                continue;

            if (!TryTeleportToMapSpawn(uid, shuttle.Comp.Station, pendingXform))
                continue;

            CompleteClockIn(uid, shuttle.Comp.Station);
            shell.WriteLine(Loc.GetString("cmd-arrivals-forced", ("uid", ToPrettyString(uid))));
        }
    }

    private void OnArrivalsFTL(EntityUid shuttleUid, ArrivalsShuttleComponent component, ref FTLStartedEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(shuttleUid, out var netComp))
            return;

        TryComp<FTLComponent>(shuttleUid, out var ftlComp);
        var ftlTime = TimeSpan.FromSeconds(ftlComp?.TravelTime ?? _shuttles.DefaultTravelTime);

        var payload = new NetworkPayload
        {
            [ShuttleTimerMasks.ShuttleMap] = shuttleUid,
            [ShuttleTimerMasks.ShuttleTime] = ftlTime,
            [ShuttleTimerMasks.SourceMap] = args.FromMapUid,
            [ShuttleTimerMasks.SourceTime] = ftlTime
        };

        if (Exists(args.TargetCoordinates.EntityId))
        {
            payload[ShuttleTimerMasks.DestMap] = Transform(args.TargetCoordinates.EntityId).MapUid;
            payload[ShuttleTimerMasks.DestTime] = ftlTime;
        }

        _deviceNetworkSystem.QueuePacket(shuttleUid, null, payload, netComp.TransmitFrequency);
    }

    private void OnArrivalsDocked(EntityUid uid, ArrivalsShuttleComponent component, ref FTLCompletedEvent args)
    {
        var onHoldingMap = args.MapUid == component.HoldingMap;
        SetShuttleDoorBolts(uid, onHoldingMap);

        if (!onHoldingMap)
            SetDockedDoorBoltLights(uid, false);

        var dockTime = component.NextTransfer > _timing.CurTime
            ? component.NextTransfer - _timing.CurTime + TimeSpan.FromSeconds(_shuttles.DefaultStartupTime)
            : TimeSpan.Zero;

        if (TryComp<DeviceNetworkComponent>(uid, out var netComp))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = uid,
                [ShuttleTimerMasks.ShuttleTime] = dockTime,
                [ShuttleTimerMasks.SourceMap] = args.MapUid,
                [ShuttleTimerMasks.SourceTime] = dockTime,
                [ShuttleTimerMasks.Docked] = true
            };
            _deviceNetworkSystem.QueuePacket(uid, null, payload, netComp.TransmitFrequency);
        }
    }

    private void OnDockUndocked(EntityUid uid, DockingComponent component, ref UndockEvent args)
    {
        if (!HasComp<ArrivalsShuttleComponent>(args.GridAUid) &&
            !HasComp<ArrivalsShuttleComponent>(args.GridBUid))
        {
            return;
        }

        if (TryComp<DoorBoltComponent>(uid, out var bolt))
            _door.SetBoltLightsEnabled((uid, bolt), true);
    }

    public void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // Only works on latejoin even if enabled.
        if (!Enabled || _ticker.RunLevel != GameRunLevel.InRound || ev.Station == null)
            return;

        if (!TryGetStationArrivalsShuttle(ev.Station.Value, out var shuttle))
            return;

        if (!TryPickShuttleSpawn(shuttle.Owner, out var spawnLoc))
            return;

        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);

        EnsureComp<PendingClockInComponent>(ev.SpawnResult.Value);
        EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);

        if (ArrivalsGodmode)
            EnsureComp<GodmodeComponent>(ev.SpawnResult.Value);

        QueueDepartureIfNeeded(shuttle);
    }

    private void SendDirections(PlayerSpawnCompleteEvent ev)
    {
        if (!Enabled || !ev.LateJoin || ev.Silent || !_pendingQuery.HasComp(ev.Mob))
            return;

        _chat.DispatchServerMessage(ev.Player, Loc.GetString("latejoin-arrivals-boarded"));
    }

    private bool TryTeleportToMapSpawn(EntityUid player, EntityUid stationId, TransformComponent? transform = null)
    {
        if (!Resolve(player, ref transform))
            return false;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new ValueList<EntityCoordinates>(32);

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                _station.GetOwningStation(uid, xform) == stationId)
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        if (possiblePositions.Count == 0)
            return false;

        _transform.SetCoordinates(player, transform, _random.Pick(possiblePositions));
        if (_actor.TryGetSession(player, out var session))
            _chat.DispatchServerMessage(session!, Loc.GetString("latejoin-arrivals-teleport-to-spawn"));

        return true;
    }

    private void OnShuttleStartup(EntityUid uid, ArrivalsShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    /// <summary>
    /// Check if an entity is still in the arrivals flow.
    /// </summary>
    public bool IsOnArrivals(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (_pendingQuery.HasComp(entity.Owner))
            return true;

        return TryGetArrivalsShuttle(entity.Comp.GridUid, out _);
    }

    public TimeSpan? NextShuttleArrival()
    {
        var query = EntityQueryEnumerator<ArrivalsShuttleComponent>();
        var time = TimeSpan.MaxValue;
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.NextArrivalsTime > _timing.CurTime && comp.NextArrivalsTime < time)
                time = comp.NextArrivalsTime;
        }

        return time == TimeSpan.MaxValue ? null : time - _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArrivalsShuttleComponent, ShuttleComponent, TransformComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var shuttle, out var xform))
        {
            ProcessPendingClockIns(uid, comp);

            if (HasComp<FTLComponent>(uid) || comp.NextTransfer > curTime)
                continue;

            var tripTime = TimeSpan.FromSeconds(_shuttles.DefaultTravelTime + _shuttles.DefaultStartupTime);
            var onHoldingMap = xform.MapUid == comp.HoldingMap;
            SetShuttleDoorBolts(uid, onHoldingMap);

            if (onHoldingMap)
            {
                if (!HasPendingOnShuttle(uid) && !HasActivePlayersOnShuttle(uid))
                {
                    comp.NextArrivalsTime = TimeSpan.Zero;
                    comp.NextTransfer = curTime + IdlePollInterval;
                    continue;
                }

                var targetGrid = _station.GetLargestGrid(comp.Station);
                if (targetGrid == null)
                {
                    comp.NextTransfer = curTime + IdlePollInterval;
                    continue;
                }

                _shuttles.FTLToDock(uid, shuttle, targetGrid.Value);
                comp.NextArrivalsTime = curTime + tripTime;
                comp.NextTransfer = curTime + tripTime + EmptyReturnDelay;
                continue;
            }

            if (HasActivePlayersOnShuttle(uid))
            {
                comp.NextTransfer = curTime + EmptyReturnDelay;
                continue;
            }

            if (!TryComp<MapComponent>(comp.HoldingMap, out var holdingMap))
            {
                comp.NextTransfer = curTime + IdlePollInterval;
                continue;
            }

            var returnCoords = new EntityCoordinates(comp.HoldingMap, Vector2.Zero);
            _shuttles.FTLToCoordinates(uid, shuttle, returnCoords, Angle.Zero, useProximity: true);
            comp.NextArrivalsTime = TimeSpan.Zero;
            comp.NextTransfer = curTime + tripTime;
            _mapSystem.SetPaused(holdingMap.MapId, false);
        }
    }

    private void ProcessPendingClockIns(EntityUid shuttleUid, ArrivalsShuttleComponent shuttle)
    {
        var query = AllEntityQuery<PendingClockInComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == shuttleUid)
                continue;

            if (xform.MapUid == shuttle.HoldingMap)
            {
                if (TryPickShuttleSpawn(shuttleUid, out var spawnLoc))
                    _transform.SetCoordinates(uid, xform, spawnLoc);

                continue;
            }

            CompleteClockIn(uid, shuttle.Station);
        }
    }

    private void CompleteClockIn(EntityUid uid, EntityUid station)
    {
        RemCompDeferred<PendingClockInComponent>(uid);
        RemCompDeferred<AutoOrientComponent>(uid);

        if (ArrivalsGodmode)
            RemCompDeferred<GodmodeComponent>(uid);

        if (_actor.TryGetSession(uid, out var session) && session is not null)
            _antag.TryMakeLateJoinAntag(session);
    }

    private bool HasPendingOnShuttle(EntityUid shuttleUid)
    {
        var query = AllEntityQuery<PendingClockInComponent, TransformComponent>();

        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == shuttleUid)
                return true;
        }

        return false;
    }

    private bool HasActivePlayersOnShuttle(EntityUid shuttleUid)
    {
        var query = AllEntityQuery<ActorComponent, MobStateComponent, TransformComponent>();

        while (query.MoveNext(out _, out _, out _, out var xform))
        {
            if (xform.GridUid == shuttleUid)
                return true;
        }

        return false;
    }

    private void SetShuttleDoorBolts(EntityUid shuttleUid, bool bolted)
    {
        var query = AllEntityQuery<DoorComponent, DoorBoltComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var door, out var bolt, out var xform))
        {
            if (xform.GridUid != shuttleUid)
                continue;

            if (bolted)
            {
                _door.SetBoltLightsEnabled((uid, bolt), true);

                if (door.State != DoorState.Closed)
                {
                    if (door.State != DoorState.Closing)
                        _door.TryClose(uid, door);

                    continue;
                }
            }

            _door.SetBoltsDown((uid, bolt), bolted);
        }
    }

    private void SetDockedDoorBoltLights(EntityUid shuttleUid, bool enabled)
    {
        var query = AllEntityQuery<DockingComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var dock, out var xform))
        {
            if (xform.GridUid != shuttleUid || dock.DockedWith == null)
                continue;

            SetDoorBoltLights(uid, enabled);
            SetDoorBoltLights(dock.DockedWith.Value, enabled);
        }
    }

    private void SetDoorBoltLights(EntityUid uid, bool enabled)
    {
        if (TryComp<DoorBoltComponent>(uid, out var bolt))
            _door.SetBoltLightsEnabled((uid, bolt), enabled);
    }

    private bool TryGetArrivalsShuttle(EntityUid? gridUid, out Entity<ArrivalsShuttleComponent> shuttle)
    {
        if (gridUid != null && TryComp<ArrivalsShuttleComponent>(gridUid.Value, out var comp))
        {
            shuttle = (gridUid.Value, comp);
            return true;
        }

        shuttle = default;
        return false;
    }

    private bool TryGetStationArrivalsShuttle(EntityUid station, out Entity<ArrivalsShuttleComponent> shuttle)
    {
        if (!TryComp<StationArrivalsComponent>(station, out var stationArrivals))
        {
            shuttle = default;
            return false;
        }

        if (Deleted(stationArrivals.Shuttle))
            SetupShuttle(station, stationArrivals);

        if (Deleted(stationArrivals.Shuttle) ||
            !TryComp<ArrivalsShuttleComponent>(stationArrivals.Shuttle, out var arrivals))
        {
            shuttle = default;
            return false;
        }

        shuttle = (stationArrivals.Shuttle, arrivals);
        return true;
    }

    private bool TryPickShuttleSpawn(EntityUid shuttleUid, out EntityCoordinates spawnLoc)
    {
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new ValueList<EntityCoordinates>(16);

        while (points.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.GridUid != shuttleUid)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0)
        {
            spawnLoc = default;
            return false;
        }

        spawnLoc = _random.Pick(possiblePositions);
        return true;
    }

    private void QueueDepartureIfNeeded(Entity<ArrivalsShuttleComponent> shuttle)
    {
        var xform = Transform(shuttle.Owner);
        if (xform.MapUid != shuttle.Comp.HoldingMap)
            return;

        var departure = _timing.CurTime + DepartureDelay;
        if (shuttle.Comp.NextTransfer < _timing.CurTime || shuttle.Comp.NextTransfer > departure)
            shuttle.Comp.NextTransfer = departure;

        shuttle.Comp.NextArrivalsTime =
            departure + TimeSpan.FromSeconds(_shuttles.DefaultStartupTime + _shuttles.DefaultTravelTime);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        if (!Enabled)
            return;

        SetupArrivalsStation();
    }

    private void SetupArrivalsStation()
    {
        var query = AllEntityQuery<StationArrivalsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SetupShuttle(uid, comp);
        }
    }

    private void SetArrivals(bool obj)
    {
        Enabled = obj;

        if (Enabled)
        {
            SetupArrivalsStation();
        }
        else
        {
            var shuttleQuery = AllEntityQuery<ArrivalsShuttleComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }

            var sourceQuery = AllEntityQuery<ArrivalsSourceComponent>();

            while (sourceQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
        }
    }

    private void OnStationPostInit(EntityUid uid, StationArrivalsComponent component, ref StationPostInitEvent args)
    {
        if (!Enabled)
            return;

        // If it's a latespawn station then this will fail but that's okey.
        SetupShuttle(uid, component);
    }

    private void SetupShuttle(EntityUid uid, StationArrivalsComponent component)
    {
        if (!Deleted(component.Shuttle))
            return;

        var holdingMap = _mapSystem.CreateMap(out var holdingMapId, runMapInit: false);
        _metaData.SetEntityName(holdingMap, Loc.GetString("map-name-terminal"));
        EnsureComp<ArrivalsSourceComponent>(holdingMap);
        _mapSystem.InitializeMap(holdingMapId);

        if (!_loader.TryLoadGrid(holdingMapId, component.ShuttlePath, out var shuttle))
        {
            _mapSystem.DeleteMap(holdingMapId);
            return;
        }

        component.Shuttle = shuttle.Value;
        var arrivalsComp = EnsureComp<ArrivalsShuttleComponent>(component.Shuttle);
        arrivalsComp.Station = uid;
        arrivalsComp.HoldingMap = holdingMap;
        arrivalsComp.NextTransfer = _timing.CurTime + IdlePollInterval;
        arrivalsComp.NextArrivalsTime = TimeSpan.Zero;

        EnsureComp<ProtectedGridComponent>(component.Shuttle);
        EnsureComp<PreventPilotComponent>(component.Shuttle);
        SetShuttleDoorBolts(component.Shuttle, true);
    }
}
