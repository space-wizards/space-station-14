using System;
using System.Collections.Generic;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring
{
    [Serializable, NetSerializable]
    public enum CrewMonitoringUIKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public class CrewMonitoringState : BoundUserInterfaceState
    {
        public List<SuitSensorStatus> Sensors;

        public CrewMonitoringState(List<SuitSensorStatus> sensors)
        {
            Sensors = sensors;
        }
    }

}
