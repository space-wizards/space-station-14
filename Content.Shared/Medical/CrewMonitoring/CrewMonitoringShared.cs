using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringState : IBoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;

    public CrewMonitoringState(List<SuitSensorStatus> sensors)
    {
        Sensors = sensors;
    }
}
