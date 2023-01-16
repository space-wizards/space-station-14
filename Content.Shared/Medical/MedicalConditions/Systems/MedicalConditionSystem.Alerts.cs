using Content.Shared.Alert;
using Content.Shared.Medical.MedicalConditions.Components;

namespace Content.Shared.Medical.MedicalConditions.Systems;

public sealed partial class MedicalConditionSystem
{
    private void UpdateAlerts(EntityUid receiverEntity, EntityUid conditionEntity,
        MedicalConditionComponent? condition = null, MedicalConditionReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver) || !Resolve(conditionEntity, ref condition))
            return;

        if (condition.Alert == null || !_prototypeManager.TryIndex<AlertPrototype>(condition.Alert, out var alert))
            return;
        //TODO: remove this when alerts get refactored to not be hardcoded.
        var alertType = alert.AlertType;
        var severity = _alerts.GetSeverityFromPercentage(alertType, condition.Severity);
        _alerts.ShowAlert(receiverEntity, alertType, severity);
    }
}
