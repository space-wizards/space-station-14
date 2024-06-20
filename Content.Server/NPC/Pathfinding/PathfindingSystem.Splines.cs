using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Gets a spline path from start to end.
    /// </summary>
    public SplinePathResult GetSplinePath(SplinePathArgs args, Random random)
    {
        var start = args.Args.Start;
        var end = args.Args.End;

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
        var cameFrom = new Dictionary<Vector2i, Vector2i>();

        for (var i = 0; i < spline.Count - 1; i++)
        {
            var point = spline[i];
            var target = spline[i + 1];
            var aStarArgs = args.Args with { Start = point, End = target };

            var aStarResult = GetPath(aStarArgs);

            if (aStarResult == SimplePathResult.NoPath)
                return SplinePathResult.NoPath;

            path.AddRange(aStarResult.Path[1..]);

            foreach (var a in aStarResult.CameFrom)
            {
                cameFrom[a.Key] = a.Value;
            }
        }

        return new SplinePathResult()
        {
            Path = path,
            CameFrom = cameFrom,
        };
    }

    public record struct SplinePathResult
    {
        public static SplinePathResult NoPath = new();

        public List<Vector2i> Path;
        public Dictionary<Vector2i, Vector2i> CameFrom;
    }

    public record struct SplinePathArgs(PathArgs Args)
    {
        public PathArgs Args = Args;

        public float MaxRatio = 0.8f;

        /// <summary>
        /// Minimum distance between subdivisions.
        /// </summary>
        public int Distance = 5;
    }
}
