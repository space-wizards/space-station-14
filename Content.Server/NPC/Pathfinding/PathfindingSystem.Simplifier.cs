using Robust.Shared.Map;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    // TODO: Re-use the existing simplifier. Because the pathfinding API sucks I just copy-pasted for now.
    public static List<TileRef> Simplify(List<TileRef> vertices, float tolerance = 0)
    {
        if (vertices.Count <= 3)
            return vertices;

        var simplified = new List<TileRef>();

        for (var i = 0; i < vertices.Count; i++)
        {
            // No wraparound for negative sooooo
            var prev = vertices[i == 0 ? vertices.Count - 1 : i - 1];
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Count];

            // If they collinear, continue
            if (IsCollinear(in prev, in current, in next, tolerance))
                continue;

            simplified.Add(current);
        }

        // Farseer didn't seem to handle straight lines and nuked all points
        if (simplified.Count == 0)
        {
            simplified.Add(vertices[0]);
            simplified.Add(vertices[^1]);
        }

        return simplified;
    }

    private static bool IsCollinear(in TileRef prev, in TileRef current, in TileRef next, float tolerance)
    {
        return FloatInRange(Area(in prev, in current, in next), -tolerance, tolerance);
    }

    private static float Area(in TileRef a, in TileRef b, in TileRef c)
    {
        return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
    }

    private static bool FloatInRange(float value, float min, float max)
    {
        return (value >= min && value <= max);
    }
}
