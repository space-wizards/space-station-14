using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring
{
    [Serializable, NetSerializable]
    public enum CrewMonitoringUIKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class CrewMonitoringState : BoundUserInterfaceState
    {
        public List<SuitSensorStatus> Sensors;
        public readonly Vector2 WorldPosition;
        public readonly bool Snap;
        public readonly float Precision;

        public CrewMonitoringState(List<SuitSensorStatus> sensors, Vector2 worldPosition, bool snap, float precision)
        {
            Sensors = sensors;
            WorldPosition = worldPosition;
            Snap = snap;
            Precision = precision;
        }
    }

}
