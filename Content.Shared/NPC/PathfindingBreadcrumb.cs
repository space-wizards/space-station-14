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

    public PathfindingBreadcrumbFlag Flags;
    public int CollisionLayer;
    public int CollisionMask;

    public static readonly PathfindingBreadcrumb Invalid = new()
    {
        Flags = PathfindingBreadcrumbFlag.Space,
        CollisionLayer = -1,
        CollisionMask = -1,
    };

    public bool IsInterior => (Flags & PathfindingBreadcrumbFlag.Interior) != 0x0;
    public bool IsBoundary => (Flags & PathfindingBreadcrumbFlag.Interior) == 0x0;

    public PathfindingBreadcrumb(Vector2i coordinates, int layer, int mask, PathfindingBreadcrumbFlag flags = PathfindingBreadcrumbFlag.None)
    {
        Coordinates = coordinates;
        CollisionLayer = layer;
        CollisionMask = mask;
        Flags = flags;
    }

    /// <summary>
    /// Is this crumb equal for pathfinding region purposes.
    /// </summary>
    public bool Equivalent(PathfindingBreadcrumb other)
    {
        return CollisionLayer.Equals(other.CollisionLayer) &&
               CollisionMask.Equals(other.CollisionMask) &&
               (Flags & PathfindingBreadcrumbFlag.Space) == (other.Flags & PathfindingBreadcrumbFlag.Space);
    }

    public bool Equals(PathfindingBreadcrumb other)
    {
        return CollisionLayer.Equals(other.CollisionLayer) &&
               CollisionMask.Equals(other.CollisionMask) &&
               Flags.Equals(other.Flags) &&
               Coordinates.Equals(other.Coordinates);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathfindingBreadcrumb other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coordinates, Flags, CollisionLayer, CollisionMask);
    }
}

[Flags]
public enum PathfindingBreadcrumbFlag : ushort
{
    None = 0,
    Space = 1 << 0,
    Interior = 1 << 1,

    /// <summary>
    /// Are we outside the bounds of our chunk. This is separate to Interior.
    /// </summary>
    IsBorder = 1 << 2,
}
