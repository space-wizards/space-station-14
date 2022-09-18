using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/// <summary>
/// An n x n square of equivalent pathfinding points. Used to determine boundary points.
/// </summary>
[Serializable, NetSerializable]
public struct PathfindingCell : IEquatable<PathfindingCell>
{
    public const int Length = 2;

    public static readonly PathfindingCell Invalid = new(PathfindingBreadcrumbFlag.None, -1, -1);

    public PathfindingData Data;

    // TODO: Wrap this in debug
    public Vector2i Indices;

    public PathfindingCell(PathfindingData data)
    {
        Data = new (data.Flags, data.CollisionLayer, data.CollisionMask);
        Indices = Vector2i.Zero;
    }

    public PathfindingCell(PathfindingBreadcrumbFlag flags, int layer, int mask)
    {
        Data = new(flags, layer, mask);
        Indices = Vector2i.Zero;
    }

    public bool Equals(PathfindingCell other)
    {
        return Indices.Equals(other.Indices) && Data.Equals(other.Data);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathfindingCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, Indices);
    }
}
