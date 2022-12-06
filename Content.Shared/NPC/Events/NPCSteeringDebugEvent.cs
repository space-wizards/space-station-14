using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Events;

/// <summary>
/// Client debug data for NPC steering
/// </summary>
[Serializable, NetSerializable]
public sealed class NPCSteeringDebugEvent : EntityEventArgs
{
    public EntityUid EntityUid;
    public Vector2 Direction;
}
