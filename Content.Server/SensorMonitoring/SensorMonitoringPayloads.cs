using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Server.SensorMonitoring;

[Serializable, NetSerializable]
public sealed partial class SensorMonitoringAtmosDataPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Payload;
}
