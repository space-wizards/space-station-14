using Content.Shared.MedicalScanner;

namespace Content.Client.HealthAnalyzer.UI.Tooltips;

public sealed partial class HeartHealthTooltip : UpdatableTooltip
{
    public override void Update(HealthAnalyzerScannedUserMessage msg)
    {
        if (msg.WoundableData is not { } woundable)
            return;

        Label.Text = Loc.GetString("health-analyzer-heart-health-tooltip", ("heartrate", woundable.HeartRate));
    }
}
