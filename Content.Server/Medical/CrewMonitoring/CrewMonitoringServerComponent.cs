using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Server.Medical.CrewMonitoring;

/// <summary>
/// Stores suit sensor status received by a crew monitoring server.
/// </summary>
[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem))]
public sealed partial class CrewMonitoringServerComponent : Component
{
    /// <summary>
    /// List of all currently connected sensors to this server.
    /// </summary>
    public readonly Dictionary<DeviceAddress, SuitSensorStatus> SensorStatus = new();

    /// <summary>
    /// How long without an update before a sensor is considered lost, in seconds.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;
}
