using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Server.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public sealed partial class BroadcastSuitSensorStatePayload : NetworkPayload
{
    [DataField]
    public Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
