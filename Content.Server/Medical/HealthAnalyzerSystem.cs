using Content.Shared.MedicalScanner;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Medical;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    protected override void UpdateUi(Entity<HealthAnalyzerComponent> analyzer, HealthAnalyzerScannedUserMessage message)
    {
        var state = new HealthAnalyzerBUIState();

        if (TryComp<TemperatureComponent>(analyzer.Comp.ScannedEntity, out var temp))
            state.Temperature = temp.CurrentTemperature;

        _ui.SetUiState(analyzer.Owner, HealthAnalyzerUiKey.Key, state);
    }
}
