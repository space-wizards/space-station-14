using System;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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

        public TimeSpan Timestamp;
        public string Name;
        public string Job;
        public bool IsAlive;
        public int? TotalDamage;
        public MapCoordinates? Coordinates;
    }

    public static class SuitSensorConstants
    {
        public const string NET_NAME = "name";
        public const string NET_JOB = "job";
        public const string NET_IS_ALIVE = "alive";
        public const string NET_TOTAL_DAMAGE = "vitals";
        public const string NET_CORDINATES = "cords";
    }
}
