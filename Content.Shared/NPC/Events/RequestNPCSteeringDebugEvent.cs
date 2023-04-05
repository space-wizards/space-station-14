using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Events;

/// <summary>
/// Raised from client to server to request NPC steering debug info.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestNPCSteeringDebugEvent : EntityEventArgs
{
    public bool Enabled;
}
