using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

public sealed partial class AtmosMonitorRegisterDevicePayload : NetworkPayloadBase<AtmosMonitorRegisterDevicePayload>;

public sealed partial class AtmosMonitorDeregisterDevicePayload : NetworkPayloadBase<AtmosMonitorDeregisterDevicePayload>;

public sealed partial class AtmosMonitorSetThresholdPayload : NetworkPayloadBase<AtmosMonitorSetThresholdPayload>
{
    [DataField]
    public AtmosMonitorThresholdType Type;

    [DataField]
    public AtmosAlarmThreshold Threshold;

    [DataField]
    public Gas? Gas;
}

public sealed partial class AtmosMonitorSetAllThresholdsPayload : NetworkPayloadBase<AtmosMonitorSetAllThresholdsPayload>
{
    [DataField]
    public AtmosMonitorDataPayload Data;
}

/// <summary>
/// Sets the alarm state of a <see cref="AtmosMonitorComponent"/> to Normal
/// and broadcasts it to all listening <see cref="AtmosAlarmableComponent"/>.
/// </summary>
public sealed partial class AtmosMonitorResetPayload : NetworkPayloadBase<AtmosMonitorResetPayload>;
