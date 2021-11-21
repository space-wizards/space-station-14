using System;
using Content.Shared.FixedPoint;
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

    }

    [Serializable, NetSerializable]
    public class CrewMonitoringStatus
    {
        public CrewMonitoringStatus(string name, string job)
        {
            Name = name;
            Job = job;
        }

        public string Name;
        public string Job;
        public bool IsAlive;
        public FixedPoint2? TotalDamage;
        public MapCoordinates? Coordinates;
    }
}
