using Content.Shared.Alert;
using Content.Shared.Medical.Symptoms.Components;

namespace Content.Shared.Medical.Symptoms.Systems;

public sealed partial class SymptomSystem
{
    private void UpdateAlerts(EntityUid receiverId, EntityUid symptomId, SymptomComponent? symptom = null)
    {
        if (!Resolve(symptomId, ref symptom))
            return;

        if (symptom.Alert == null || !_prototypeManager.TryIndex<AlertPrototype>(symptom.Alert, out var alert))
            return;

        var alertType = alert.AlertType;
        var severity = _alerts.GetSeverityFromPercentage(alertType, symptom.Severity);
        _alerts.ShowAlert(receiverId, alertType, severity);
    }

    private void HideAlert(EntityUid receiverId, EntityUid symptomId, SymptomComponent? symptom = null)
    {
        if (!Resolve(symptomId, ref symptom))
            return;

        if (symptom.Alert == null || !_prototypeManager.TryIndex<AlertPrototype>(symptom.Alert, out var alert))
            return;

        var alertType = alert.AlertType;
        _alerts.ClearAlert(receiverId, alertType);
    }
}
