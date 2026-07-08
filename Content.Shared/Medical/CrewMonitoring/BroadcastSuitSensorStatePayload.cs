using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Shared.Medical.CrewMonitoring;

public sealed partial class BroadcastSuitSensorStatePayload : NetworkPayloadBase<BroadcastSuitSensorStatePayload>
{
    [DataField]
    public Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
