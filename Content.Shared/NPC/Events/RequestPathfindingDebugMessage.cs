using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class RequestPathfindingDebugMessage : EntityEventArgs
{
    public PathfindingDebugMode Mode;
}
