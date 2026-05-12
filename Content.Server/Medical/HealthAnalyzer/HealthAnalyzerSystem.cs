using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;

namespace Content.Server.Medical.HealthAnalyzer;

public sealed partial class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    protected override void UpdateUi(Entity<HealthAnalyzerComponent> analyzer)
    {
        var state = GetHealthAnalyzerUiState(analyzer.Comp.ScannedEntity, analyzer.Comp.IsAnalyzerActive);

        UI.SetUiState(analyzer.Owner, HealthAnalyzerUiKey.Key, state);
    }
}
