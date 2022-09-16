using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public struct PathfindingBreadcrumb
{
    public Vector2 Coordinates;
    public bool IsSpace;
    public int CollisionLayer;
    public int CollisionMask;
}
