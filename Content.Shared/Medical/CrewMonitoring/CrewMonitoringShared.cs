using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

/// <summary>
/// Sensor data and station context sent to the crew monitoring UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrewMonitoringState(List<SuitSensorStatus> sensors, NetEntity? stationUid) : BoundUserInterfaceState
{
    /// <summary>
    /// Current status of sensors known to the console.
    /// </summary>
    public List<SuitSensorStatus> Sensors = sensors;

    /// <summary>
    /// Owning station of the console, or null when it is not on a station.
    /// </summary>
    public NetEntity? StationUid = stationUid;
}
