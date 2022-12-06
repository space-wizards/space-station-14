using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Events;

/// <summary>
/// Client debug data for NPC steering
/// </summary>
[Serializable, NetSerializable]
public sealed class NPCSteeringDebugEvent : EntityEventArgs
{
    public List<NPCSteeringDebugData> Data;

    public NPCSteeringDebugEvent(List<NPCSteeringDebugData> data)
    {
        Data = data;
    }
}

[Serializable, NetSerializable]
public readonly struct NPCSteeringDebugData
{
    public readonly EntityUid EntityUid;
    public readonly Vector2 Direction;
    public readonly float[] DangerMap;
    public readonly float[] InterestMap;

    public NPCSteeringDebugData(EntityUid entityUid, Vector2 direction, float[] dangerMap, float[] interestMap)
    {
        EntityUid = entityUid;
        Direction = direction;
        DangerMap = dangerMap;
        InterestMap = interestMap;
    }
}
