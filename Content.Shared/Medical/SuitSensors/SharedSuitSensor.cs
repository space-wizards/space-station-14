using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.SuitSensor;

[Serializable, NetSerializable]
public sealed class SuitSensorStatus
{
    public SuitSensorStatus(NetEntity ownerUid, NetEntity suitSensorUid, string name, string job, string jobIcon, List<string> jobDepartments)
    {
        OwnerUid = ownerUid;
        SuitSensorUid = suitSensorUid;
        Name = name;
        Job = job;
        JobIcon = jobIcon;
        JobDepartments = jobDepartments;
    }

    public TimeSpan Timestamp;
    public NetEntity SuitSensorUid;
    public NetEntity OwnerUid;
    public string Name;
    public string Job;
    public string JobIcon;
    public List<string> JobDepartments;
    public bool IsAlive;
    public int? TotalDamage;
    public int? TotalDamageThreshold;
    public float? DamagePercentage => TotalDamageThreshold == null || TotalDamage == null ? null : TotalDamage / (float) TotalDamageThreshold;
    public NetCoordinates? Coordinates;
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
    public const string NET_OWNER_UID = "ownerUid";
    public const string NET_NAME = "name";
    public const string NET_JOB = "job";
    public const string NET_JOB_ICON = "jobIcon";
    public const string NET_JOB_DEPARTMENTS = "jobDepartments";
    public const string NET_IS_ALIVE = "alive";
    public const string NET_TOTAL_DAMAGE = "vitals";
    public const string NET_TOTAL_DAMAGE_THRESHOLD = "vitalsThreshold";
    public const string NET_COORDINATES = "coords";
    public const string NET_SUIT_SENSOR_UID = "uid";

    ///Used by the CrewMonitoringServerSystem to send the status of all connected suit sensors to each crew monitor
    public const string NET_STATUS_COLLECTION = "suit-status-collection";
}

[Serializable, NetSerializable]
public sealed partial class SuitSensorChangeDoAfterEvent : DoAfterEvent
{
    public SuitSensorMode Mode { get; private set; } = SuitSensorMode.SensorOff;

    public SuitSensorChangeDoAfterEvent(SuitSensorMode mode)
    {
        Mode = mode;
    }

    public override DoAfterEvent Clone() => this;
}
