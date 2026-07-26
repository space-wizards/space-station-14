using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Broadcast payoad from the crew monitoring server to all crew monitors.
/// </summary>
public sealed partial class BroadcastSuitSensorStatePayload : NetworkPayloadBase<BroadcastSuitSensorStatePayload>
{
    [DataField]
    public Dictionary<string, SuitSensorStatus> SensorStatus = new();
}
