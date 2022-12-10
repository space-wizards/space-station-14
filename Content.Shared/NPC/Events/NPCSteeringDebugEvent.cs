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
    public readonly NPCSteeringContext Context;
    public readonly List<Vector2> DangerPoints;

    public NPCSteeringDebugData(EntityUid entityUid, Vector2 direction, NPCSteeringContext context, List<Vector2> dangerPoints)
    {
        EntityUid = entityUid;
        Direction = direction;
        Context = context;
        DangerPoints = dangerPoints;
    }
}
