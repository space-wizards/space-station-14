using Content.Shared.DeviceNetwork;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.SuitSensors;

/// <summary>
/// A network payload that contains <see cref="SuitSensorStatus"/>.
/// </summary>
public sealed partial class SuitSensorStatusPayload : NetworkPayloadBase<SuitSensorStatusPayload>
{
    [DataField]
    public SuitSensorStatus Data;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct SuitSensorStatus : IEquatable<SuitSensorStatus>
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

    public bool Equals(SuitSensorStatus other)
    {
        return Timestamp.Equals(other.Timestamp)
               && SuitSensorUid.Equals(other.SuitSensorUid)
               && OwnerUid.Equals(other.OwnerUid)
               && Name == other.Name
               && Job == other.Job
               && JobIcon == other.JobIcon
               && IsAlive == other.IsAlive
               && TotalDamage == other.TotalDamage
               && TotalDamageThreshold == other.TotalDamageThreshold
               && Nullable.Equals(Coordinates, other.Coordinates);
    }

    public override bool Equals(object? obj)
    {
        return obj is SuitSensorStatus other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Timestamp);
        hashCode.Add(SuitSensorUid);
        hashCode.Add(OwnerUid);
        hashCode.Add(Name);
        hashCode.Add(Job);
        hashCode.Add(JobIcon);
        hashCode.Add(IsAlive);
        hashCode.Add(TotalDamage);
        hashCode.Add(TotalDamageThreshold);
        hashCode.Add(Coordinates);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(SuitSensorStatus left, SuitSensorStatus right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SuitSensorStatus left, SuitSensorStatus right)
    {
        return !left.Equals(right);
    }
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
