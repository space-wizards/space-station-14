using Robust.Shared.Serialization;

namespace Content.Shared.Alert;

[Serializable, NetSerializable]
public sealed class AlertsComponentState : ComponentState
{
    public Dictionary<AlertKey, AlertState> Alerts;

    public AlertsComponentState(Dictionary<AlertKey, AlertState> alerts)
    {
        Alerts = alerts;
    }
}
