using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.GameTicking;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Spawners.Components;
using Content.Shared.Spawners.EntitySystems;
using Content.Shared.Station.Systems;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
namespace Content.Shared.Shuttles.Systems;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class ArrivalsSystem : EntitySystem
{
    [Dependency] protected IConfigurationManager Cfg = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] protected GameTicker Ticker = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    /// <summary>
    /// If enabled then spawns players on an alternate map so they can take a shuttle to the station.
    /// </summary>
    public bool Enabled { get; protected set; }

    /// <summary>
    /// Flags if all players spawning at the departure terminal have godmode until they leave the terminal.
    /// </summary>
    public bool ArrivalsGodmode { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArrivalsShuttleComponent, ComponentStartup>(OnShuttleStartup);

        // Don't invoke immediately as it will get set in the natural course of things.
        Enabled = Cfg.GetCVar(CCVars.ArrivalsShuttles);
        ArrivalsGodmode = Cfg.GetCVar(CCVars.GodmodeArrivals);

        Cfg.OnValueChanged(CCVars.GodmodeArrivals, b => ArrivalsGodmode = b);
    }

    [SubscribeLocalEvent(before: new []{typeof(SpawnPointSystem), typeof(ContainerSpawnPointSystem)})]
    public void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // We use arrivals as the default spawn so don't check for job prio.

        // Only works on latejoin even if enabled.
        if (!Enabled || Ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (!HasComp<StationArrivalsComponent>(ev.Station))
            return;

        TryGetArrivals(out var arrivals);

        if (!TryComp(arrivals, out TransformComponent? arrivalsXform))
            return;

        var mapId = arrivalsXform.MapID;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.MapID != mapId)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count <= 0)
            return;

        var spawnLoc = Random.Pick(possiblePositions);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);

        EnsureComp<PendingClockInComponent>(ev.SpawnResult.Value);
        EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);

        // If you're forced to spawn, you're invincible until you leave wherever you were forced to spawn.
        if (ArrivalsGodmode)
            EnsureComp<GodmodeComponent>(ev.SpawnResult.Value);
    }

    private void OnShuttleStartup(EntityUid uid, ArrivalsShuttleComponent component, ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    [PublicAPI]
    public bool TryGetArrivals(out EntityUid uid)
    {
        var arrivalsQuery = EntityQueryEnumerator<ArrivalsSourceComponent>();

        while (arrivalsQuery.MoveNext(out uid, out _))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if an entity is on the arrivals grid.
    /// </summary>
    /// <param name="entity">Entity to check.</param>
    /// <returns>True if the entity is on the arrivals grid. Returns false if not on arrivals, or there is no arrivals grid.</returns>
    [PublicAPI]
    public bool IsOnArrivals(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!TryGetArrivals(out var arrivals))
            return false;

        var arrivalsGridUid = Transform(arrivals).GridUid;
        if (!arrivalsGridUid.HasValue)
            return false;

        return entity.Comp.GridUid == Transform(arrivals).GridUid;
    }

    public TimeSpan? NextShuttleArrival()
    {
        var query = EntityQueryEnumerator<ArrivalsShuttleComponent>();
        var time = TimeSpan.MaxValue;
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.NextArrivalsTime < time)
                time = comp.NextArrivalsTime;
        }

        var duration = Timing.CurTime;
        return (time < duration) ? null : time - duration;
    }
}
