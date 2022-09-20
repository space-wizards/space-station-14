using Content.Shared.NPC;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    public float EuclideanDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return MathF.Sqrt((dx * dx + dy * dy));
    }

    /// <summary>
    /// Assumes same graph.
    /// </summary>
    public float ManhattanDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return dx + dy;
    }

    /// <summary>
    /// Assumes same graph.
    /// </summary>
    public float OctileDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return dx + dy + (1.41f - 2) * Math.Min(dx, dy);
    }

    private Vector2 GetDiff(PathPoly start, PathPoly end)
    {
        // TODO: Numerics when we changeover.
        var diff = start.Box.Center - end.Box.Center;
        var ab = Vector2.Abs(diff);
        return ab;
    }
}
