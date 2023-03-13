using Content.Shared.NPC;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    public float EuclideanDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return MathF.Sqrt((dx * dx + dy * dy));
    }

    public float ManhattanDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return dx + dy;
    }

    public float ManhattanDistance(Vector2i start, Vector2i end)
    {
        var distance = end - start;
        return Math.Abs(distance.X) + Math.Abs(distance.Y);
    }

    public float OctileDistance(PathPoly start, PathPoly end)
    {
        var (dx, dy) = GetDiff(start, end);
        return dx + dy + (1.41f - 2) * Math.Min(dx, dy);
    }

    public float OctileDistance(Vector2i start, Vector2i end)
    {
        var diff = start - end;
        var ab = Vector2.Abs(diff);
        return ab.X + ab.Y + (1.41f - 2) * Math.Min(ab.X, ab.Y);
    }

    private Vector2 GetDiff(PathPoly start, PathPoly end)
    {
        var startPos = start.Box.Center;
        var endPos = end.Box.Center;

        if (end.GraphUid != start.GraphUid)
        {
            if (!TryComp<TransformComponent>(start.GraphUid, out var startXform) ||
                !TryComp<TransformComponent>(end.GraphUid, out var endXform))
            {
                return Vector2.Zero;
            }

            endPos = startXform.InvWorldMatrix.Transform(endXform.WorldMatrix.Transform(endPos));
        }

        // TODO: Numerics when we changeover.
        var diff = startPos - endPos;
        var ab = Vector2.Abs(diff);
        return ab;
    }
}
