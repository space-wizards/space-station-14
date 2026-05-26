using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.RoundEnd;

/// <summary>
///     Forces a shuttle call when any grid with <see cref="ShuttleCallerFailsafeComponent"/> has nothing with
///     <see cref="ShuttleCallerComponent"/> on it, or on the same map if IncludeCallersInSameMap is true.
/// </summary>
public sealed partial class ShuttleCallerFailsafeSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configMan = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private EmergencyShuttleSystem _shuttleSys = default!;
    [Dependency] private RoundEndSystem _roundEndSys = default!;

    public static readonly LocId AnnouncementText = "round-end-system-shuttle-called-failsafe-announcement";
    private bool _shuttleEnabled;
    private bool _failsafeEnabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<ShuttleCallerComponent, EntityTerminatingEvent>(OnShuttleCallerTerminating);
        SubscribeLocalEvent<ShuttleCallerComponent, MapInitEvent>(OnShuttleCallerInit);
        SubscribeLocalEvent<ShuttleCallerComponent, GridUidChangedEvent>(OnShuttleCallerGridChange);
        SubscribeLocalEvent<ShuttleCallerComponent, MapUidChangedEvent>(OnShuttleCallerMapChange);

        Subs.CVar(_configMan, CCVars.EmergencyShuttleEnabled, value => _shuttleEnabled = value, true);
        Subs.CVar(_configMan, CCVars.EmergencyShuttleCallerFailsafeEnabled, value => _failsafeEnabled = value, true);
    }

    private void OnShuttleCallerTerminating(Entity<ShuttleCallerComponent> uid, ref EntityTerminatingEvent args)
    {
        GridChanging(uid, _transformSystem.GetGrid(uid.Owner), null);
        MapChanging(uid, _transformSystem.GetMap(uid.Owner), null);
    }

    private void OnStationPostInit(ref StationPostInitEvent args)
    {
        foreach (var uid in args.Station.Comp.Grids)
        {
            EnsureComp<ShuttleCallerFailsafeComponent>(uid);
        }
    }

    private void OnShuttleCallerInit(Entity<ShuttleCallerComponent> uid, ref MapInitEvent args)
    {
        var thisGrid = _transformSystem.GetGrid(uid.Owner);
        var thisMap = _transformSystem.GetMap(uid.Owner);

        if (TryComp<ShuttleCallerFailsafeComponent>(thisGrid, out var comp))
        {
            AddToGrid(uid, (thisGrid.Value, comp));
        }

        MapChanging(uid, null, thisMap);
    }

    private void OnShuttleCallerGridChange(Entity<ShuttleCallerComponent> uid, ref GridUidChangedEvent args)
    {
        GridChanging(uid, _transformSystem.GetGrid(uid.Owner), args.NewGrid);
    }

    private void OnShuttleCallerMapChange(Entity<ShuttleCallerComponent> uid, ref MapUidChangedEvent args)
    {
        MapChanging(uid, _transformSystem.GetMap(uid.Owner), args.NewMap);
    }

    private void GridChanging(Entity<ShuttleCallerComponent> uid, EntityUid? oldGrid, EntityUid? newGrid)
    {
        if (TryComp<ShuttleCallerFailsafeComponent>(oldGrid, out var oldGridComp)
            && !oldGridComp.IncludeCallersInSameMap)
        {
            RemoveFromGrid(uid, (oldGrid.Value, oldGridComp));
        }
        if (TryComp<ShuttleCallerFailsafeComponent>(newGrid, out var newGridComp))
        {
            AddToGrid(uid, (newGrid.Value, newGridComp));
        }
    }

    private void MapChanging(Entity<ShuttleCallerComponent> uid, EntityUid? oldMap, EntityUid? newMap)
    {
        var failsafeQuery = EntityQueryEnumerator<ShuttleCallerFailsafeComponent>();

        while (failsafeQuery.MoveNext(out var grid, out var comp))
        {
            if (!comp.IncludeCallersInSameMap)
            {
                continue;
            }
            var gridMap = _transformSystem.GetMap(grid);
            if (gridMap == oldMap)
            {
                RemoveFromGrid(uid, (grid, comp));
            }
            if (gridMap == newMap)
            {
                AddToGrid(uid, (grid, comp));
            }
        }
    }

    private void AddToGrid(EntityUid caller, Entity<ShuttleCallerFailsafeComponent> grid)
        => grid.Comp.Callers.Add(caller);

    private void RemoveFromGrid(EntityUid caller, Entity<ShuttleCallerFailsafeComponent> grid)
    {
        grid.Comp.Callers.Remove(caller);
        ShuttleCheck(grid);
    }

    private void ShuttleCheck(Entity<ShuttleCallerFailsafeComponent> uid)
    {
        if (uid.Comp.Callers.Count > 0)
        {
            return;
        }

        // We check _failsafeEnabled here, because it could be updated midround.
        if (!_shuttleEnabled || !_failsafeEnabled)
        {
            return;
        }

        if (_shuttleSys.ShuttlesLeft || _shuttleSys.EmergencyShuttleArrived ||
            _roundEndSys.ExpectedCountdownEnd != null)
        {
            return; // The shuttle is either already called, here, or has left.
        }

        _roundEndSys.RequestRoundEnd(text: AnnouncementText);
    }
}
