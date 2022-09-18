using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/// <summary>
/// An n x n square of equivalent pathfinding points. Used to determine boundary points.
/// </summary>
[Serializable, NetSerializable]
public sealed class PathfindingCell
{
    public const int Length = 2;

    public static readonly PathfindingCell Invalid = new(PathfindingBreadcrumbFlag.None, -1, -1);

    public PathfindingData Data;

    public PathfindingCell(PathfindingData data)
    {
        Data = new (data.Flags, data.CollisionLayer, data.CollisionMask);
    }

    public PathfindingCell(PathfindingBreadcrumbFlag flags, int layer, int mask)
    {
        Data = new(flags, layer, mask);
    }
}
