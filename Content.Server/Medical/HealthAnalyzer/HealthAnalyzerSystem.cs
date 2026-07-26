using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Shared.Temperature.Components;

namespace Content.Server.Medical.HealthAnalyzer;

public sealed partial class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    public override HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target, bool scanMode)
    {
        var state = base.GetHealthAnalyzerUiState(target, scanMode);

        if (TryComp<TemperatureComponent>(target, out var temp))
            state.Temperature = temp.Temperature;

        return state;
    }
}
