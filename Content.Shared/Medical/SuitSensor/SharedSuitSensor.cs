using System;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.SuitSensor
{
    [Serializable, NetSerializable]
    public class SuitSensorStatus
    {
        public SuitSensorStatus(string name, string job)
        {
            Name = name;
            Job = job;
        }

        public string Name;
        public string Job;
        public bool IsAlive;
        public int? TotalDamage;
        public MapCoordinates? Coordinates;
    }
}
