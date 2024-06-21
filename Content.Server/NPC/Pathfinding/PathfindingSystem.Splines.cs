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
        var pairs = new ValueList<(Vector2i Start, Vector2i End)> { (start, end) };

        // Sub-divide recursively
        while (remaining > args.Distance)
        {
            remaining /= 2f;

            var i = 0;

            while (i < pairs.Count)
            {
                var pointA = pairs[i].Start;
                var pointB = pairs[i].End;
                var vector = pointB - pointA;
                var halfway = vector / 2f;

                // Finding the point
                var adj = halfway.Length();
                var opposite = args.MaxRatio * adj;
                var hypotenuse = MathF.Sqrt(MathF.Pow(adj, 2) + MathF.Pow(opposite, 2));

                // Okay so essentially we have 2 points and no poly
                // We add 2 other points to form a diamond and want some point halfway between randomly offset.
                var angle = new Angle(MathF.Atan(opposite / adj));
                var pointAPerp = pointA + angle.RotateVec(halfway).Normalized() * hypotenuse;
                var pointBPerp = pointA + (-angle).RotateVec(halfway).Normalized() * hypotenuse;

                var perpLine = pointBPerp - pointAPerp;
                var perpHalfway = perpLine.Length() / 2f;

                var splinePoint = (pointAPerp + perpLine.Normalized() * random.NextFloat(-args.MaxRatio, args.MaxRatio) * perpHalfway).Floored();

                // We essentially take (A, B) and turn it into (A, C) & (C, B)
                pairs[i] = (pointA, splinePoint);
                pairs.Insert(i + 1, (splinePoint, pointB));

                i+= 2;
            }
        }

        var spline = new ValueList<Vector2i>(pairs.Count - 1)
        {
            start
        };

        foreach (var pair in pairs)
        {
            spline.Add(pair.End);
        }

        // Now we need to pathfind between each node on the spline.

        // TODO: Add rotation version or straight-line version for pathfinder config
        // Move the worm pathfinder to here I think.
        var cameFrom = new Dictionary<Vector2i, Vector2i>();

        // TODO: Need to get rid of the branch bullshit.

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

        public float MaxRatio = 0.25f;

        /// <summary>
        /// Minimum distance between subdivisions.
        /// </summary>
        public int Distance = 5;
    }
}
