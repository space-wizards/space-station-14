using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// Used for synchronizing the sender device and adding its address to all listeners.
/// </summary>
public partial record struct AtmosMonitorRegisterDevicePayload : INetworkPayload;

/// <summary>
/// Removes the sender device from all listeners of this payload.
/// </summary>
public partial record struct AtmosMonitorDeregisterDevicePayload : INetworkPayload;

/// <summary>
/// Sets specific threshold on the target atmos device.
/// </summary>
public partial record struct AtmosMonitorSetThresholdPayload : INetworkPayload
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
public partial record struct AtmosMonitorSetAllThresholdsPayload : INetworkPayload
{
    [DataField]
    public AtmosMonitorData Data;
}

/// <summary>
/// Sets the alarm state of a <see cref="AtmosMonitorComponent"/> to Normal
/// and broadcasts it to all listening <see cref="AtmosAlarmableComponent"/>.
/// </summary>
public partial record struct AtmosMonitorResetPayload : INetworkPayload;
