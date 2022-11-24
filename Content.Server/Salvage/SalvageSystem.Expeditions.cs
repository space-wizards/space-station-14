using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private const int MissionLimit = 5;

    private void InitializeExpeditions()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnSalvageExpStationInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ComponentInit>(OnSalvageExpInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, EntParentChangedMessage>(OnSalvageExpParent);
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
    }

    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.AvailableMissions.Clear();

        // TODO: Random time
        // TODO: sealed record
        for (var i = 0; i < MissionLimit; i++)
        {
            var mission = new SalvageMission()
            {
                Index = component.NextIndex,
                MissionType = SalvageMissionType.Structure,
            };

            component.AvailableMissions[component.NextIndex++] = mission;
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
        return new SalvageExpeditionConsoleState(component.AvailableMissions.Values.ToList());
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
            state = new SalvageExpeditionConsoleState(new List<SalvageMission>());
        }

        _ui.TrySetUiState(component.Owner, SalvageConsoleUiKey.Expedition, state);
    }
}
