using System.Linq;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.Station.Systems;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private const int MissionLimit = 5;

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

        // TODO: Mark it as claimed.
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

        // TODO: sealed record
        for (var i = 0; i < MissionLimit; i++)
        {
            var mission = new SalvageMission()
            {
                Index = component.NextIndex,
                MissionType = SalvageMissionType.Structure,
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
        var seed = new Random(mission.Seed);
        var mapId = _mapManager.CreateMap();
        var grid = EnsureComp<MapGridComponent>(_mapManager.GetMapEntityId(mapId));

        // No point raising an event for this IG considering it's just gonna ba procgen thing?
        SalvageJob job;

        switch (mission.Environment)
        {
            case SalvageEnvironment.Caves:
                // CA
                job = new SalvageJob(0.005);
                break;
            default:
                return;
        }

        _salvageQueue.EnqueueJob(job);
    }
}
