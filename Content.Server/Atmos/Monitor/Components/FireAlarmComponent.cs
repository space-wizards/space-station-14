namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed partial class FireAlarmComponent : Component
{
    [DataField("alarmTriggered")]
    public bool AlarmTriggered = false;
}
