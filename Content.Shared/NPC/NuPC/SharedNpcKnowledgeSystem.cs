using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC.NuPC;

public abstract class SharedNpcKnowledgeSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed class RequestNpcKnowledgeEvent : EntityEventArgs
{
    public bool Enabled;
}

[Serializable, NetSerializable]
public sealed class NpcKnowledgeDebugEvent : EntityEventArgs
{
    public List<NpcKnowledgeData> Data = new();
}

[Serializable, NetSerializable]
public record struct NpcKnowledgeData
{
    public NetEntity Entity;
    public List<NpcSensor> Sensors;

    // Stims
    public List<HostileMobStim> HostileMobs;
    public List<LastKnownHostilePositionStim> LastHostileMobPositions;
}

/// <summary>
/// Represents a visible hostile mob.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial record struct HostileMobStim
{
    [NonSerialized]
    [DataField]
    public EntityUid Owner;

    /*
     * Networked data only used for debugging.
     */

    public NetEntity DebugOwner;

    public bool Equals(HostileMobStim other)
    {
        return Owner.Equals(other.Owner);
    }

    public override int GetHashCode()
    {
        return Owner.GetHashCode();
    }
}

/// <summary>
/// Represents the last position we saw a hostile mob.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial record struct LastKnownHostilePositionStim : INpcTimedStim
{
    [field: NonSerialized]
    [DataField]
    public TimeSpan EndTime { get; set; }

    [NonSerialized]
    [DataField]
    public EntityUid Owner;

    [NonSerialized]
    [DataField]
    public EntityCoordinates Coordinates;

    /*
     * Networked data only used for debugging.
     */

    public NetEntity DebugOwner;
    public NetCoordinates DebugCoordinates;

    public bool Equals(LastKnownHostilePositionStim other)
    {
        return Owner.Equals(other.Owner) && Coordinates.Equals(other.Coordinates);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Owner.GetHashCode() * 397) ^ Coordinates.GetHashCode();
        }
    }
}

public interface INpcTimedStim
{
    /// <summary>
    /// Time when the stim runs out and no longer matters.
    /// </summary>
    public TimeSpan EndTime { get; set; }
}

[Serializable, NetSerializable]
public enum NpcSensorFlag : byte
{
    HostileMobs,
}

/// <summary>
/// Handles what an NPC can "see" / "hear" in the game.
/// Every update these get iterated and are integral to keeping <see cref="NpcKnowledgeComponent"/> up to date.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class NpcSensor
{
    /// <summary>
    /// Shape of this sensor.
    /// </summary>
    [DataField(required: true)]
    public IPhysShape Shape = new PhysShapeCircle(10f);

    /// <summary>
    /// Should we check if the target stim is InRangeUnobstructed
    /// </summary>
    [DataField]
    public bool Unoccluded = false;

    /// <summary>
    /// What stims this sensor can react to.
    /// </summary>
    [DataField(required: true)]
    public List<NpcSensorFlag> ValidStims = new();
}
