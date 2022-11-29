using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.Salvage.Expeditions;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private const int MissionLimit = 5;

    private const double SalvageGenTime = 0.005;
    private readonly JobQueue _salvageQueue = new();

    private void InitializeExpeditions()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnSalvageExpStationInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ComponentInit>(OnSalvageExpInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, EntParentChangedMessage>(OnSalvageExpParent);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ClaimSalvageMessage>(OnSalvageClaimMessage);
    }

    private void OnSalvageClaimMessage(EntityUid uid, SalvageExpeditionConsoleComponent component, ClaimSalvageMessage args)
    {
        var station = _station.GetOwningStation(uid);

        if (!TryComp<SalvageExpeditionDataComponent>(station, out var data) || data.Claimed)
            return;

        if (!data.Missions.TryGetValue(args.Index, out var mission))
            return;

        SpawnMission(mission);

        data.ActiveMission = args.Index;
        UpdateConsoles(data);
    }

    private void OnSalvageExpInit(EntityUid uid, SalvageExpeditionConsoleComponent component, ComponentInit args)
    {
        UpdateConsole(component);
    }

    private void OnSalvageExpParent(EntityUid uid, SalvageExpeditionConsoleComponent component, ref EntParentChangedMessage args)
    {
        UpdateConsole(component);
    }

    private void OnSalvageExpStationInit(StationInitializedEvent ev)
    {
        EnsureComp<SalvageExpeditionDataComponent>(ev.Station);
    }

    private void UpdateExpeditions()
    {
        var currentTime = _timing.CurTime;

        foreach (var comp in EntityQuery<SalvageExpeditionDataComponent>())
        {
            // Update offers
            if (comp.NextOffer < currentTime)
            {
                comp.NextOffer += comp.Cooldown;
                GenerateMissions(comp);
                UpdateConsoles(comp);
            }
        }

        _salvageQueue.Process();
    }

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.Missions.Clear();
        int timeBlock = 15;
        var configs = _prototypeManager.EnumeratePrototypes<SalvageExpeditionPrototype>().ToArray();

        if (configs.Length == 0)
            return;

        // TODO: sealed record
        for (var i = 0; i < MissionLimit; i++)
        {
            var mission = new SalvageMission()
            {
                Index = component.NextIndex,
                Config = _random.Pick(configs).ID,
                Seed = _random.Next(),
                Duration = TimeSpan.FromSeconds(_random.Next(9 * 60 / timeBlock, 12 * 60 / timeBlock) * timeBlock),
            };

            component.Missions[component.NextIndex++] = mission;
        }
    }

    private void UpdateConsoles(SalvageExpeditionDataComponent component)
    {
        var state = GetState(component);

        foreach (var (console, xform, uiComp) in EntityQuery<SalvageExpeditionConsoleComponent, TransformComponent, ServerUserInterfaceComponent>(true))
        {
            var station = _station.GetOwningStation(console.Owner, xform);

            if (station != component.Owner)
                continue;

            _ui.TrySetUiState(console.Owner, SalvageConsoleUiKey.Expedition, state, ui: uiComp);
        }
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.Claimed, component.ActiveMission, missions);
    }

    private void UpdateConsole(SalvageExpeditionConsoleComponent component)
    {
        var station = _station.GetOwningStation(component.Owner);
        SalvageExpeditionConsoleState state;

        if (TryComp<SalvageExpeditionDataComponent>(station, out var dataComponent))
        {
            state = GetState(dataComponent);
        }
        else
        {
            state = new SalvageExpeditionConsoleState(false, 0, new List<SalvageMission>());
        }

        _ui.TrySetUiState(component.Owner, SalvageConsoleUiKey.Expedition, state);
    }

    private void SpawnMission(SalvageMission mission)
    {
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _mapManager.AddUninitializedMap(mapId);
        MetaDataComponent? metadata = null;
        var grid = EnsureComp<MapGridComponent>(_mapManager.GetMapEntityId(mapId));

        var parallax = EnsureComp<ParallaxComponent>(mapUid);
        parallax.Parallax = "Grass";
        Dirty(parallax, metadata);
        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        Dirty(gravity, metadata);
        EnsureComp<MapLightComponent>(mapUid);
        var atmos = EnsureComp<MapAtmosphereComponent>(mapUid);

        atmos.Space = false;
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        atmos.Mixture = new GasMixture(2500)
        {
            Temperature = 293.15f,
            Moles = moles,
        };

        var footstep = EnsureComp<FootstepModifierComponent>(mapUid);
        footstep.Sound = new SoundCollectionSpecifier("FootstepGrass");
        Dirty(footstep, metadata);
        _mapManager.DoMapInitialize(mapId);

        // No point raising an event for this when it's 1-1.
        SalvageJob job;
        var config = _prototypeManager.Index<SalvageExpeditionPrototype>(mission.Config);

        // TODO: Need to generate mission objectives.
        // TODO: Spawn hint markers for spawns.

        switch (config.Environment)
        {
            case SalvageCaveGen cave:
                job = GetCaveJob(grid.Owner, grid, config, cave, mission.Seed);
                break;
            default:
                return;
        }

        _salvageQueue.EnqueueJob(job);
    }
}
