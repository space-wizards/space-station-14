using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Chat.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.CallERT;

public sealed class StationCallErtSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);

    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        if (!TryComp<StationCallErtComponent>(args.Station, out var callErtComponent))
            return;

        if (!_prototypeManager.TryIndex(callErtComponent.ErtGroupsPrototype, out ErtGroupsPrototype? ertGroups))
        {
            return;
        }

        callErtComponent.ErtGroups = ertGroups;
    }


    private bool TryAddShuttle(string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        var shuttleMap = _mapManager.CreateMap();

        if (!_map.TryLoad(shuttleMap, shuttlePath, out var gridList))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        }

        //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
        if (gridList.Count != 1)
        {
            switch (gridList.Count)
            {
                case < 1:
                    _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, no grid found in file");
                    break;
                case > 1:
                {
                    _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, too many grids present in file");

                    foreach (var grid in gridList)
                    {
                        _mapManager.DeleteGrid(grid);
                    }

                    break;
                }
            }

            return false;
        }

        shuttleGrid = gridList[0];
        return true;
    }


    private bool CheckCallErt(EntityUid stationUid, ErtGroupDetail ertGroupDetails, StationCallErtComponent component)
    {
        var totalHumans = GetHumans(stationUid).Count;
        var totalZombies = GetZombies(stationUid).Count;
        var totalDeadHumans = GetDeadHumans(stationUid).Count;
        var totalPudles = GetPuddles(stationUid).Count;
        var totalDangerAtmos = GetDangerAtmos(stationUid).Count;
        var roundDuration = _gameTiming.CurTime.TotalMinutes;

        if (component.NewCallErtCooldownRemaining > 0)
        {
            return false;
        }

        if (ertGroupDetails.Requirements.TryGetValue("RoundDuration", out var requirementDuration))
        {
            if (!(roundDuration >= requirementDuration))
                return false;
        }

        if (ertGroupDetails.Requirements.TryGetValue("DeadPercent", out var requirementDead))
        {
            var deadPercent = totalDeadHumans / (float)totalHumans * 100;
            if (deadPercent >= requirementDead)
                return true;
        }

        if (ertGroupDetails.Requirements.TryGetValue("ZombiePercent", out var requirementZombie))
        {
            var zombiePercent = totalZombies / (float)totalHumans * 100;
            if (zombiePercent >= requirementZombie)
                return true;
        }

        if (ertGroupDetails.Requirements.TryGetValue("PuddlesCount", out var requirementPudles))
        {
            if (totalPudles >= requirementPudles)
                return true;
        }

        if (ertGroupDetails.Requirements.TryGetValue("DangerAlarmCount", out var requirementAlarms))
        {
            if (totalDangerAtmos >= requirementAlarms)
                return true;
        }

        return true;
    }


    private List<EntityUid> GetHumans(EntityUid stationUid)
    {
        var humans = new List<EntityUid>();
        var humansQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        while (humansQuery.MoveNext(out var uid, out _, out var mob))
        {
            if (_mobState.IsAlive(uid, mob))
            {
                var owningStationUid = _stationSystem.GetOwningStation(uid);
                if (owningStationUid == stationUid)
                    humans.Add(uid);
            }
        }
        return humans;
    }


    private List<EntityUid> GetZombies(EntityUid stationUid)
    {
        var healthy = new List<EntityUid>();
        var zombies = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, ZombieComponent>();
        while (zombies.MoveNext(out var uid, out _, out var mob, out var zombie))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                healthy.Add(uid);
        }
        return healthy;
    }


    private List<EntityUid> GetPuddles(EntityUid stationUid)
    {
        var pudles = new List<EntityUid>();
        var pudlesQuery = AllEntityQuery<PuddleComponent>();
        while (pudlesQuery.MoveNext(out var uid, out var puddle))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                pudles.Add(uid);
        }
        return pudles;
    }


    private List<EntityUid> GetDangerAtmos(EntityUid stationUid)
    {
        var dangerAtmos = new List<EntityUid>();
        var atmosAlarmableQuery = AllEntityQuery<AtmosAlarmableComponent>();
        while (atmosAlarmableQuery.MoveNext(out var uid, out var atmosAlarmable))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid != stationUid)
                continue;
            if (atmosAlarmable.LastAlarmState == AtmosAlarmType.Danger)
                dangerAtmos.Add(uid);
        }
        return dangerAtmos;
    }


    private List<EntityUid> GetDeadHumans(EntityUid stationUid)
    {
        var deadHumans = new List<EntityUid>();
        var players = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (_mobState.IsDead(uid, mob))
            {
                var owningStationUid = _stationSystem.GetOwningStation(uid);
                if (owningStationUid == stationUid)
                    deadHumans.Add(uid);
            }
        }
        return deadHumans;
    }


    public void CallErt(EntityUid stationUid, string ertGroup, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null
            || !component.ErtGroups.ErtGroupList.TryGetValue(ertGroup, out var ertGroupDetails)
           )
        {
            return;
        }

        var message = "";
        message += $"{Loc.GetString($"ert-call-announcement-{ertGroup}")} ";

        if (!CheckCallErt(stationUid, ertGroupDetails, component))
        {
            message += Loc.GetString("ert-call-refusal-announcement");
            _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
                colorOverride: Color.Gold);
            return;
        }

        component.CalledErtGroup = ertGroupDetails;
        component.CallErtCooldownRemaining = ertGroupDetails.WaitingTime;
        component.ErtCalled = true;
        message += Loc.GetString($"ert-call-accepted-announcement");

        _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
            colorOverride: Color.Gold);

        RaiseLocalEvent(new CallErtEvent(stationUid));
    }


    public bool ReallErt(EntityUid stationUid, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null
            || component.CalledErtGroup == null)
        {
            return false;
        }

        if (!component.ErtCalled)
            return false;

        component.ErtCalled = false;
        component.CallErtCooldownRemaining = 0;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-recall-announcement",
                ("name", Loc.GetString($"ert-group-full-name-{component.CalledErtGroup.Name}"))), playSound: true,
            colorOverride: Color.Gold);

        RaiseLocalEvent(new RecallErtEvent(stationUid));
        return true;
    }


    public TimeSpan? TimeToErt(EntityUid? stationUid, MetaDataComponent? dataComponent = null, StationCallErtComponent? component = null)
    {
        if (stationUid == null)
            return null;

        if (!Resolve(stationUid.Value, ref component, ref dataComponent))
        {
            return null;
        }

        if (!component.ErtCalled)
        {
            return null;
        }

        return _gameTiming.CurTime + TimeSpan.FromSeconds(component.CallErtCooldownRemaining);
    }

    public void SpawnErt(ErtGroupDetail? ertGroupDetails, StationCallErtComponent component)
    {
        if (ertGroupDetails == null)
            return;

        if (component.ErtGroups == null)
            return;

        if (!TryAddShuttle(ertGroupDetails.ShuttlePath, out var shuttleGrid))
            return;
        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, xform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != shuttleGrid)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(shuttleGrid.Value).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }

        foreach (var human in ertGroupDetails.HumansList)
        {
            for (var i = 0; i < human.Value; i++)
            {
                EntityManager.SpawnEntity(human.Key, _random.Pick(spawns));
            }
        }

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-spawn-announcement",
                ("name", Loc.GetString($"ert-group-full-name-{ertGroupDetails.Name}"))), playSound: true,
            colorOverride: Color.Gold);
        component.NewCallErtCooldownRemaining = 1200;
    }

    public override void Update(float frameTime)
    {
        foreach (var comp in EntityQuery<StationCallErtComponent>())
        {
            if (comp.NewCallErtCooldownRemaining >= 0f)
            {
                comp.NewCallErtCooldownRemaining -= frameTime;
            }

            if (comp.ErtCalled)
            {
                if (comp.CallErtCooldownRemaining >= 0f)
                {
                    comp.CallErtCooldownRemaining -= frameTime;
                }

                if (comp.CallErtCooldownRemaining <= 0)
                {
                    comp.CallErtCooldownRemaining = 0;
                    // spawn ert
                    SpawnErt(comp.CalledErtGroup, comp);
                    comp.ErtCalled = false;
                }
            }
        }

        base.Update(frameTime);
    }
}


public sealed class CallErtEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public CallErtEvent(EntityUid station)
    {
        Station = station;
    }
}


public sealed class RecallErtEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public RecallErtEvent(EntityUid station)
    {
        Station = station;
    }
}
