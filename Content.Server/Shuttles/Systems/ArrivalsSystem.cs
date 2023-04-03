using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Mobs.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Spawners.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// If enabled spawns players on a separate arrivals station before they can transfer to the main station.
/// </summary>
public sealed class ArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// If enabled then spawns players on an alternate map so they can take a shuttle to the station.
    /// </summary>
    public bool Enabled { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawn, before: new []{typeof(SpawnPointSystem)});
        SubscribeLocalEvent<StationArrivalsComponent, ComponentStartup>(OnArrivalsStartup);

        SubscribeLocalEvent<ArrivalsShuttleComponent, ComponentStartup>(OnShuttleStartup);
        SubscribeLocalEvent<ArrivalsShuttleComponent, EntityUnpausedEvent>(OnShuttleUnpaused);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);

        SubscribeLocalEvent<StationInitializedEvent>(OnStationInit);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<ArrivalsShuttleComponent, FTLStartedEvent>(OnArrivalsFTL);

        // Don't invoke immediately as it will get set in the natural course of things.
        Enabled = _cfgManager.GetCVar(CCVars.ArrivalsShuttles);
        _cfgManager.OnValueChanged(CCVars.ArrivalsShuttles, SetArrivals);

        // Command so admins can set these for funsies
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
                var query = AllEntityQuery<PendingClockInComponent, TransformComponent>();
                var spawnPoints = EntityQuery<SpawnPointComponent, TransformComponent>().ToList();

                TryGetArrivals(out var arrivalsUid);

                while (query.MoveNext(out var uid, out _, out var pendingXform))
                {
                    _random.Shuffle(spawnPoints);

                    foreach (var (point, xform) in spawnPoints)
                    {
                        if (point.SpawnType != SpawnPointType.LateJoin || xform.GridUid == arrivalsUid)
                            continue;

                        _transform.SetCoordinates(uid, pendingXform, xform.Coordinates);
                        break;
                    }

                    RemCompDeferred<PendingClockInComponent>(uid);
                    shell.WriteLine(Loc.GetString("cmd-arrivals-forced", ("uid", ToPrettyString(uid))));
                }
                break;
            default:
                shell.WriteError(Loc.GetString($"cmd-arrivals-invalid"));
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(CCVars.ArrivalsShuttles, SetArrivals);
    }

    private void OnArrivalsFTL(EntityUid uid, ArrivalsShuttleComponent component, ref FTLStartedEvent args)
    {
        // Any mob then yeet them off the shuttle.
        if (!_cfgManager.GetCVar(CCVars.ArrivalsReturns) && args.FromMapUid != null)
        {
            var pendingEntQuery = GetEntityQuery<PendingClockInComponent>();
            var arrivalsBlacklistQuery = GetEntityQuery<ArrivalsBlacklistComponent>();
            var mobQuery = GetEntityQuery<MobStateComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            DumpChildren(uid, ref args, pendingEntQuery, arrivalsBlacklistQuery, mobQuery, xformQuery);
        }

        var pendingQuery = AllEntityQuery<PendingClockInComponent, TransformComponent>();

        // Clock them in when they FTL
        while (pendingQuery.MoveNext(out var pUid, out _, out var xform))
        {
            // Cheaper to iterate pending arrivals than all children
            if (xform.GridUid != uid)
                continue;

            RemCompDeferred<PendingClockInComponent>(pUid);
        }
    }

    private void DumpChildren(EntityUid uid,
        ref FTLStartedEvent args,
        EntityQuery<PendingClockInComponent> pendingEntQuery,
        EntityQuery<ArrivalsBlacklistComponent> arrivalsBlacklistQuery,
        EntityQuery<MobStateComponent> mobQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        if (pendingEntQuery.HasComponent(uid))
            return;

        var xform = xformQuery.GetComponent(uid);

        if (mobQuery.HasComponent(uid) || arrivalsBlacklistQuery.HasComponent(uid))
        {
            var rotation = xform.LocalRotation;
            _transform.SetCoordinates(uid, new EntityCoordinates(args.FromMapUid!.Value, args.FTLFrom.Transform(xform.LocalPosition)));
            _transform.SetWorldRotation(uid, args.FromRotation + rotation);
            return;
        }

        var children = xform.ChildEnumerator;

        while (children.MoveNext(out var child))
        {
            DumpChildren(child.Value, ref args, pendingEntQuery, arrivalsBlacklistQuery, mobQuery, xformQuery);
        }
    }

    private void OnStationInit(StationInitializedEvent ev)
    {
        EnsureComp<StationArrivalsComponent>(ev.Station);
    }

    private void OnPlayerSpawn(PlayerSpawningEvent ev)
    {
        // Only works on latejoin even if enabled.
        if (!Enabled || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        var points = EntityQuery<SpawnPointComponent, TransformComponent>().ToList();
        _random.Shuffle(points);
        TryGetArrivals(out var arrivals);

        if (TryComp<TransformComponent>(arrivals, out var arrivalsXform))
        {
            var mapId = arrivalsXform.MapID;

            foreach (var (spawnPoint, xform) in points)
            {
                if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.MapID != mapId)
                    continue;

                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
                    xform.Coordinates,
                    ev.Job,
                    ev.HumanoidCharacterProfile,
                    ev.Station);

                EnsureComp<PendingClockInComponent>(ev.SpawnResult.Value);
                return;
            }
        }
    }

    private void OnShuttleStartup(EntityUid uid, ArrivalsShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    private void OnShuttleUnpaused(EntityUid uid, ArrivalsShuttleComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTransfer += args.PausedTime;
    }

    private bool TryGetArrivals(out EntityUid uid)
    {
        var arrivalsQuery = EntityQueryEnumerator<ArrivalsSourceComponent>();

        while (arrivalsQuery.MoveNext(out uid, out _))
        {
            return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArrivalsShuttleComponent, ShuttleComponent, TransformComponent>();
        var curTime = _timing.CurTime;
        TryGetArrivals(out var arrivals);

        // TODO: FTL fucker, if on an edge tile every N seconds check for wall or w/e
        // TODO: Docking should be per-grid rather than per dock and bump off when undocking.

        // TODO: Stop dispatch if emergency shuttle has arrived.
        // TODO: Need server join message specifying shuttle wait time or smth.
        // TODO: Need maps
        // TODO: Need emergency suits on shuttle probs
        // TODO: Need some kind of comp to shunt people off if they try to get on?
        if (TryComp<TransformComponent>(arrivals, out var arrivalsXform))
        {
            while (query.MoveNext(out var uid, out var comp, out var shuttle, out var xform))
            {
                if (comp.NextTransfer > curTime || !TryComp<StationDataComponent>(comp.Station, out var data))
                    continue;

                // Go back to arrivals source
                if (xform.MapUid != arrivalsXform.MapUid)
                {
                    if (arrivals.IsValid())
                        _shuttles.FTLTravel(uid, shuttle, arrivals, dock: true);
                }
                // Go to station
                else
                {
                    var targetGrid = _station.GetLargestGrid(data);

                    if (targetGrid != null)
                        _shuttles.FTLTravel(uid, shuttle, targetGrid.Value, dock: true);
                }

                comp.NextTransfer += TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
            }
        }
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // Setup arrivals station
        if (!Enabled)
            return;

        SetupArrivalsStation();
    }

    private void SetupArrivalsStation()
    {
        var mapId = _mapManager.CreateMap();

        if (!_loader.TryLoad(mapId, _cfgManager.GetCVar(CCVars.ArrivalsMap), out var uids))
        {
            return;
        }

        foreach (var id in uids)
        {
            EnsureComp<ArrivalsSourceComponent>(id);
            EnsureComp<ProtectedGridComponent>(id);
            EnsureComp<PreventPilotComponent>(id);
        }

        // Handle roundstart stations.
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
            var query = AllEntityQuery<StationArrivalsComponent>();

            while (query.MoveNext(out var sUid, out var comp))
            {
                SetupShuttle(sUid, comp);
            }
        }
        else
        {
            var sourceQuery = AllEntityQuery<ArrivalsSourceComponent>();

            while (sourceQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }

            var shuttleQuery = AllEntityQuery<ArrivalsShuttleComponent>();

            while (shuttleQuery.MoveNext(out var uid, out _))
            {
                QueueDel(uid);
            }
        }
    }

    private void OnArrivalsStartup(EntityUid uid, StationArrivalsComponent component, ComponentStartup args)
    {
        if (!Enabled)
            return;

        // If it's a latespawn station then this will fail but that's okey
        SetupShuttle(uid, component);
    }

    private void SetupShuttle(EntityUid uid, StationArrivalsComponent component)
    {
        if (!Deleted(component.Shuttle))
            return;

        // Spawn arrivals on a dummy map then dock it to the source.
        var dummyMap = _mapManager.CreateMap();

        if (TryGetArrivals(out var arrivals) &&
            _loader.TryLoad(dummyMap, component.ShuttlePath.ToString(), out var shuttleUids))
        {
            component.Shuttle = shuttleUids[0];
            var shuttleComp = Comp<ShuttleComponent>(component.Shuttle);
            var arrivalsComp = EnsureComp<ArrivalsShuttleComponent>(component.Shuttle);
            arrivalsComp.Station = uid;
            EnsureComp<ProtectedGridComponent>(uid);
            _shuttles.FTLTravel(component.Shuttle, shuttleComp, arrivals, hyperspaceTime: 10f, dock: true);
            arrivalsComp.NextTransfer = _timing.CurTime + TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.ArrivalsCooldown));
        }

        // Don't start the arrivals shuttle immediately docked so power has a time to stabilise?
        var timer = AddComp<TimedDespawnComponent>(_mapManager.GetMapEntityId(dummyMap));
        timer.Lifetime = 15f;
    }
}
