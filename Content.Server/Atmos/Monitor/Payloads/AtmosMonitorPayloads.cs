using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Monitor.Payloads;

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorRegisterDevicePayload : HandledNetworkPayload;

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorDeregisterDevicePayload : HandledNetworkPayload;


[Serializable, NetSerializable]
public sealed partial class AtmosMonitorSetThresholdPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosMonitorThresholdType Type;

    [DataField]
    public AtmosAlarmThreshold Threshold;

    [DataField]
    public Gas? Gas;
}

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorSetAllThresholdsPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosMonitorDataPayload Data;
}

/// <summary>
/// Sets the alarm state of a <see cref="AtmosMonitorComponent"/> to Normal
/// and broadcasts it to all listening <see cref="AtmosAlarmableComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AtmosMonitorResetPayload : HandledNetworkPayload;
