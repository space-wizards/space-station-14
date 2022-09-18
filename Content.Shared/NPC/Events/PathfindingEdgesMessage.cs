using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathfindingEdgesMessage : EntityEventArgs
{
    public EntityUid GridUid;
    public Vector2i Origin;
    public List<PathfindingBoundary> Edges = new();
}
