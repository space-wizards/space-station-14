using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Broadcast payoad from the crew monitoring server to all crew monitors.
/// </summary>
public partial record struct BroadcastSuitSensorStatePayload : INetworkPayload
{
    [DataField]
    public Dictionary<DeviceAddress, SuitSensorStatus> SensorStatus = new();
}
