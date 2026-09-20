using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringState(List<SuitSensorStatus> sensors, NetEntity? stationUid) : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors = sensors;

    public NetEntity? StationUid = stationUid;
}
