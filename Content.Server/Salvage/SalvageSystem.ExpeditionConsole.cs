using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void OnSalvageClaimMessage(EntityUid uid, SalvageExpeditionConsoleComponent component, ClaimSalvageMessage args)
    {
        var station = _station.GetOwningStation(uid);

        if (!TryComp<SalvageExpeditionDataComponent>(station, out var data) || data.Claimed)
            return;

        if (!data.Missions.TryGetValue(args.Index, out var missionparams))
            return;

        SpawnMission(missionparams, station.Value);

        data.ActiveMission = args.Index;
        var mission = GetMission(_prototypeManager.Index<SalvageDifficultyPrototype>(missionparams.Difficulty), missionparams.Seed);
        data.NextOffer = _timing.CurTime + mission.Duration + TimeSpan.FromSeconds(1);
        UpdateConsoles(data);
    }

    private void OnSalvageConsoleInit(EntityUid uid, SalvageExpeditionConsoleComponent component, ComponentInit args)
    {
        UpdateConsole(component);
    }

    private void OnSalvageConsoleParent(EntityUid uid, SalvageExpeditionConsoleComponent component, ref EntParentChangedMessage args)
    {
        UpdateConsole(component);
    }

    private void UpdateConsoles(SalvageExpeditionDataComponent component)
    {
        var state = GetState(component);

        foreach (var (console, xform, uiComp) in EntityQuery<SalvageExpeditionConsoleComponent, TransformComponent, UserInterfaceComponent>(true))
        {
            var station = _station.GetOwningStation(console.Owner, xform);

            if (station != component.Owner)
                continue;

            _ui.TrySetUiState(console.Owner, SalvageConsoleUiKey.Expedition, state, ui: uiComp);
        }
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
            state = new SalvageExpeditionConsoleState(TimeSpan.Zero, false, true, 0, new List<SalvageMissionParams>());
        }

        _ui.TrySetUiState(component.Owner, SalvageConsoleUiKey.Expedition, state);
    }
}
