using Content.Shared.MedicalScanner;

namespace Content.Client.HealthAnalyzer.UI.Tooltips;

public sealed partial class BrainHealthTooltip : UpdatableTooltip
{
    public override void Update(HealthAnalyzerScannedUserMessage msg)
    {
        if (msg.WoundableData is not { } woundable)
            return;

        Label.Text = Loc.GetString("health-analyzer-brain-health-tooltip", ("dead", woundable.BrainHealth <= 0), ("spo2", $"{woundable.Spo2 * 100:F1}"));
    }
}
