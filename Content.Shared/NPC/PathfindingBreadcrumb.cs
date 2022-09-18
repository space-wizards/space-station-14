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

    public PathfindingData Data;

    public static readonly PathfindingBreadcrumb Invalid = new()
    {
        Data = new PathfindingData(PathfindingBreadcrumbFlag.None, -1, -1),
    };

    public bool IsInterior => (Data.Flags & PathfindingBreadcrumbFlag.Interior) != 0x0;
    public bool IsBoundary => (Data.Flags & PathfindingBreadcrumbFlag.Interior) == 0x0;

    public PathfindingBreadcrumb(Vector2i coordinates, int layer, int mask, PathfindingBreadcrumbFlag flags = PathfindingBreadcrumbFlag.None)
    {
        Coordinates = coordinates;
        Data = new PathfindingData(flags, layer, mask);
    }

    /// <summary>
    /// Is this crumb equal for pathfinding region purposes.
    /// </summary>
    public bool Equivalent(PathfindingBreadcrumb other)
    {
        return Data.Equals(other.Data);
    }

    public bool Equals(PathfindingBreadcrumb other)
    {
        return Coordinates.Equals(other.Coordinates) && Data.Equals(other.Data);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathfindingBreadcrumb other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coordinates, Data);
    }
}

/// <summary>
/// The data relevant for pathfinding.
/// </summary>
[Serializable, NetSerializable]
public struct PathfindingData : IEquatable<PathfindingData>
{
    public PathfindingBreadcrumbFlag Flags;
    public int CollisionLayer;
    public int CollisionMask;

    public PathfindingData(PathfindingBreadcrumbFlag flag, int layer, int mask)
    {
        Flags = flag;
        CollisionLayer = layer;
        CollisionMask = mask;
    }

    public bool Equals(PathfindingData other)
    {
        return CollisionLayer.Equals(other.CollisionLayer) &&
               CollisionMask.Equals(other.CollisionMask) &&
               (Flags & PathfindingBreadcrumbFlag.Space) == (other.Flags & PathfindingBreadcrumbFlag.Space);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathfindingData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) Flags, CollisionLayer, CollisionMask);
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
    External = 1 << 2,
}
