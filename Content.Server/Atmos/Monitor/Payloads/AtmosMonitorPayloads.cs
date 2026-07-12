using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// Used for synchronizing the sender device and adding its address to all listeners.
/// </summary>
public sealed partial class AtmosMonitorRegisterDevicePayload : NetworkPayloadBase<AtmosMonitorRegisterDevicePayload>;

/// <summary>
/// Removes the sender device from all listeners of this payload.
/// </summary>
public sealed partial class AtmosMonitorDeregisterDevicePayload : NetworkPayloadBase<AtmosMonitorDeregisterDevicePayload>;

/// <summary>
/// Sets specific threshold on the target atmos device.
/// </summary>
public sealed partial class AtmosMonitorSetThresholdPayload : NetworkPayloadBase<AtmosMonitorSetThresholdPayload>
{
    [DataField]
    public AtmosMonitorThresholdType Type;

    [DataField]
    public AtmosAlarmThreshold Threshold;

    [DataField]
    public Gas? Gas;
}

/// <summary>
/// Sets thresholds on all connected devices.
/// </summary>
public sealed partial class AtmosMonitorSetAllThresholdsPayload : NetworkPayloadBase<AtmosMonitorSetAllThresholdsPayload>
{
    [DataField]
    public AtmosMonitorData Data;
}

/// <summary>
/// Sets the alarm state of a <see cref="AtmosMonitorComponent"/> to Normal
/// and broadcasts it to all listening <see cref="AtmosAlarmableComponent"/>.
/// </summary>
public sealed partial class AtmosMonitorResetPayload : NetworkPayloadBase<AtmosMonitorResetPayload>;
