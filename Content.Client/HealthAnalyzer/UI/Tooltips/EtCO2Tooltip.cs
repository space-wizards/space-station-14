using Content.Shared.MedicalScanner;

namespace Content.Client.HealthAnalyzer.UI.Tooltips;

public sealed partial class EtCO2Tooltip : UpdatableTooltip
{
    public override void Update(HealthAnalyzerScannedUserMessage msg)
    {
        if (msg.WoundableData is not { } woundable)
            return;

        Label.Text = Loc.GetString("health-analyzer-etco2-tooltip", ("gas", woundable.Etco2GasName), ("etco2", woundable.Etco2Name));
    }
}
