using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public struct PathfindingBreadcrumb : IEquatable<PathfindingBreadcrumb>
{
    /// <summary>
    /// The X and Y index in the point grid.
    /// The actual coordinates require using <see cref="SharedPathfindingSystem.ChunkSize"/> and <see cref="SharedPathfindingSystem.SubStep"/>
    /// </summary>
    public Vector2i Coordinates;
    public bool IsSpace;
    public int CollisionLayer;
    public int CollisionMask;

    public static readonly PathfindingBreadcrumb Invalid = new()
    {
        IsSpace = true,
        CollisionLayer = -1,
        CollisionMask = -1,
    };

    /// <summary>
    /// Is this crumb equal (apart from coordinates).
    /// </summary>
    public bool Equivalent(PathfindingBreadcrumb other)
    {
        return CollisionLayer.Equals(other.CollisionLayer) &&
               CollisionMask.Equals(other.CollisionMask) &&
               IsSpace.Equals(other.IsSpace);
    }

    public bool Equals(PathfindingBreadcrumb other)
    {
        return CollisionLayer.Equals(other.CollisionLayer) &&
               CollisionMask.Equals(other.CollisionMask) &&
               IsSpace.Equals(other.IsSpace) &&
               Coordinates.Equals(other.Coordinates);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathfindingBreadcrumb other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coordinates, IsSpace, CollisionLayer, CollisionMask);
    }
}
