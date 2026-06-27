using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;

namespace Content.Server.SensorMonitoring;

public sealed partial class SensorMonitoringAtmosDataPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Payload;
}
