using Content.Server.Atmos.Monitor.Components;

namespace Content.Server.Atmos.Monitor.Systems;

/// <summary>
///     System that alarms AtmosAlarmables via DeviceNetwork.
///     This is one way, and is usually triggered by an event.
/// </summary>
public sealed class AtmosAlarmingSystem : EntitySystem
{
    /// <summary>
    ///     The alarm command key.
    /// </summary>
    public const string AtmosAlarmCmd = "atmos_alarming_alarm_cmd";

    /// <summary>
    ///     Register command. Registers this address so that the alarm can send
    ///     to the given device.
    /// </summary>
    public const string AtmosAlarmRegisterCmd = "atmos_alarming_register_cmd";

    /// <summary>
    ///     Alarm data. Contains the alert passed into this alarmer.
    /// </summary>
    public const string AtmosAlarmData = "atmos_alarming_alarm_data";

    private void OnAlert(EntityUid uid, AtmosAlarmingComponent component, AtmosMonitorAlarmEvent args)
    {

    }
}
