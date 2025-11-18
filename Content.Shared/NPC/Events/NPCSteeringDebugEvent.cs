using System.Numerics;
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
public readonly record struct NPCSteeringDebugData(
    NetEntity EntityUid,
    Vector2 Direction,
    float[] Interest,
    float[] Danger,
    List<Vector2> DangerPoints)
{
    public readonly NetEntity EntityUid = EntityUid;
    public readonly Vector2 Direction = Direction;
    public readonly float[] Interest = Interest;
    public readonly float[] Danger = Danger;
    public readonly List<Vector2> DangerPoints = DangerPoints;
}
