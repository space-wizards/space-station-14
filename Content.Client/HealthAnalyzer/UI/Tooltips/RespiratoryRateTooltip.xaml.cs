using Content.Shared.MedicalScanner;

namespace Content.Client.HealthAnalyzer.UI.Tooltips;

public sealed partial class RespiratoryRateTooltip : UpdatableTooltip
{
    public override void Update(HealthAnalyzerScannedUserMessage msg)
    {
        if (msg.WoundableData is not { } woundable)
            return;

        Label.Text = Loc.GetString("health-analyzer-respiratory-rate-tooltip", ("etco2gas", woundable.Etco2GasName), ("etco2", woundable.Etco2Name), ("spo2gas", woundable.Spo2GasName), ("spo2", woundable.Spo2Name));
    }
}
