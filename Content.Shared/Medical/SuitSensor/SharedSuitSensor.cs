using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.SuitSensor
{
    [Serializable, NetSerializable]
    public sealed class SuitSensorStatus
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
        public EntityCoordinates? Coordinates;
    }

    [Serializable, NetSerializable]
    public enum SuitSensorMode : byte
    {
        /// <summary>
        /// Sensor doesn't send any information about owner
        /// </summary>
        SensorOff = 0,

        /// <summary>
        /// Sensor sends only binary status (alive/dead)
        /// </summary>
        SensorBinary = 1,

        /// <summary>
        /// Sensor sends health vitals status
        /// </summary>
        SensorVitals = 2,

        /// <summary>
        /// Sensor sends vitals status and GPS position
        /// </summary>
        SensorCords = 3
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
