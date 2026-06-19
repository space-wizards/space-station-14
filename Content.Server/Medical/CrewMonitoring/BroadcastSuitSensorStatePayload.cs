using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class BroadcastSuitSensorStatePayload : NetworkPayload
{
    [DataField]
    public Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
