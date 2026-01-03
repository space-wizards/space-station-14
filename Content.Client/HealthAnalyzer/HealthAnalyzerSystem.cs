using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;

namespace Content.Client.Medical.HealthAnalyzer;

public sealed class HealthAnalyzerSystem : SharedHealthAnalyzerSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    protected override void UpdateUi(Entity<HealthAnalyzerComponent> analyzer, HealthAnalyzerScannedUserMessage message)
    {
        if (!_ui.TryGetOpenUi(analyzer.Owner, HealthAnalyzerUiKey.Key, out var bui))
            return;

        _ui.RaiseUiMessage(analyzer.Owner, HealthAnalyzerUiKey.Key, message);
        bui.Update();
    }
}
