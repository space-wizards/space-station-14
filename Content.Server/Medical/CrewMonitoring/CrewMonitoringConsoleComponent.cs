using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;

namespace Content.Server.Medical.CrewMonitoring;

/// <summary>
/// Stores suit sensor status received by a crew monitoring console.
/// </summary>
[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    /// List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<DeviceAddress, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    /// How long without an update before a sensor is considered lost, in seconds.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;
}
