using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;

namespace Content.Server.Medical.HealthAnalyzer;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    protected override void UpdateUi(Entity<HealthAnalyzerComponent> analyzer)
    {
        var state = GetHealthAnalyzerUiState(analyzer.Comp.ScannedEntity, analyzer.Comp.IsAnalyzerActive);
        var message = new HealthAnalyzerScannedUserMessage(state);

        UI.ServerSendUiMessage(analyzer.Owner, HealthAnalyzerUiKey.Key, message);
    }
}
