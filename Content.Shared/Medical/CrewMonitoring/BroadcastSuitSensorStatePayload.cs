using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public sealed partial class BroadcastSuitSensorStatePayload : HandledNetworkPayload
{
    [DataField]
    public Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
