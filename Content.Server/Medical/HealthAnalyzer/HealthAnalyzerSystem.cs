using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Shared.Temperature.Components;

namespace Content.Server.Medical.HealthAnalyzer;

public sealed partial class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    /// <summary>
    /// Creates a HealthAnalyzerState based on the current state of an entity.
    /// </summary>
    /// <param name="target">The entity being scanned</param>
    /// <returns>Returns a <see cref="HealthAnalyzerUiState"/> with a valid temperature.</returns>
    public override HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target, bool scanMode)
    {
        var state = base.GetHealthAnalyzerUiState(target, scanMode);

        if (TryComp<TemperatureComponent>(target, out var temp))
            state.Temperature = temp.CurrentTemperature;

        return state;
    }
}
