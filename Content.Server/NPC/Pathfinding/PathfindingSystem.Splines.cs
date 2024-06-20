using System.Text;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Gets a spline path from start to end.
    /// </summary>
    public List<Vector2i> GetSplinePath(SplinePathArgs args, Random random)
    {
        var start = args.Start;
        var end = args.End;

        var frontier = new PriorityQueue<Vector2i, float>();
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var path = new List<Vector2i>();

        var distance = (end - start).Length;
        var remaining = distance;
        var pairs = new ValueList<(Vector2i Start, Vector2i End)>();
        pairs.Add((start, end));
        var spline = new ValueList<Vector2i>()
        {
            start,
        };

        // Sub-divide recursively
        while (remaining > args.Distance)
        {
            // Essentially we need a point half-way between Points A and B.
            // We then pick somewhere randomly perpendicular to this line and sub-divide there.

            var pointA = pairs[0].Start;
            var pointB = pairs[0].End;
            var vector = pointB - pointA;
            var halfway = vector / 2f;

            // Finding the point

            // B / cos(alpha) = C

            var C = (float) (halfway.Length() / Math.Cos(Angle.FromDegrees(45)));

            var pointAPerp = pointA + Angle.FromDegrees(45).RotateVec(halfway.Normalized() * C);
            var pointBPerp = pointA - Angle.FromDegrees(45).RotateVec(halfway);
            var perpLine = pointBPerp - pointAPerp;

            var minRatio = 1f - args.MaxRatio;
            var maxRatio = args.MaxRatio;

            var splinePoint = (random.NextFloat(minRatio, maxRatio) * perpLine).Floored();

            spline.Add(splinePoint);

            pairs.Add((pointA, splinePoint));
            pairs.Add((splinePoint, pointB));
            remaining -= halfway.Length();

            // Yes shuffle but uhh alternative is a stack
            pairs.RemoveAt(0);
        }

        spline.Add(end);

        // Now we need to pathfind between each node on the spline.

        // TODO: Add rotation version or straight-line version for pathfinder config
        // Move the worm pathfinder to here I think.
        var count = 0;

        for (var i = 0; i < spline.Count - 1; i++)
        {
            var point = spline[i];
            var target = spline[i + 1];
            frontier.Clear();
            frontier.Enqueue(point, 0f);
            cameFrom.Clear();
            var found = false;

            // TODO: Generic pathfinder method instead of this.
            while (frontier.TryDequeue(out var node, out _) && count < args.Limit)
            {
                if (node == target)
                {
                    found = true;
                    // Found target
                    break;
                }

                if (args.Diagonals)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            // TODO: G score + f score and shit.
                            // Move A* pathfinder to common method.
                            // Needs way to get tile-node costs dynamically too for walls and shit
                            var neighbor = node + new Vector2i(x, y);
                            frontier.Enqueue(neighbor, 0f);
                        }
                    }
                }
                else
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (x != 0 && y != 0)
                                continue;

                            // TODO: G score + f score and shit.
                            var neighbor = node + new Vector2i(x, y);
                            frontier.Enqueue(neighbor, 0f);
                        }
                    }
                }
            }

            if (found)
                continue;

            // TODO: Abort.
        }

        return path;
    }

    public record struct SplinePathArgs()
    {
        public Vector2i Start;
        public Vector2i End;

        /// <summary>
        /// Can we get diagonal neighbors.
        /// </summary>
        public bool Diagonals = false;

        public float MaxRatio = 0.8f;

        /// <summary>
        /// Minimum distance between subdivisions.
        /// </summary>
        public int Distance = 5;

        public int Limit = 10000;
    }
}
