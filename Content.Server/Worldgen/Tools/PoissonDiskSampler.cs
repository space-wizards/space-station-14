using Robust.Shared.Random;

namespace Content.Server._Citadel.Worldgen.Tools;

/// <summary>
///     An implementation of Poisson Disk Sampling, for evenly spreading points across a given area.
/// </summary>
public sealed class PoissonDiskSampler
{
    public const int DefaultPointsPerIteration = 30;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    ///     Samples for points within the given circle.
    /// </summary>
    /// <param name="center">Center of the sample</param>
    /// <param name="radius">Radius of the sample</param>
    /// <param name="minimumDistance">Minimum distance between points</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>A list of points</returns>
    public List<Vector2> SampleCircle(Vector2 center, float radius, float minimumDistance,
        int pointsPerIteration = DefaultPointsPerIteration)
    {
        return Sample(center - new Vector2(radius, radius), center + new Vector2(radius, radius), radius,
            minimumDistance, pointsPerIteration);
    }

    /// <summary>
    ///     Samples for points within the given rectangle.
    /// </summary>
    /// <param name="topLeft">The top left of the rectangle</param>
    /// <param name="lowerRight">The bottom right of the rectangle</param>
    /// <param name="minimumDistance">Minimum distance between points</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>A list of points</returns>
    public List<Vector2> SampleRectangle(Vector2 topLeft, Vector2 lowerRight, float minimumDistance,
        int pointsPerIteration = DefaultPointsPerIteration)
    {
        return Sample(topLeft, lowerRight, null, minimumDistance, pointsPerIteration);
    }

    /// <summary>
    ///     Samples for points within the given rectangle, with an optional rejection distance.
    /// </summary>
    /// <param name="topLeft">The top left of the rectangle</param>
    /// <param name="lowerRight">The bottom right of the rectangle</param>
    /// <param name="rejectionDistance">The distance at which points will be discarded, if any</param>
    /// <param name="minimumDistance">Minimum distance between points</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>A list of points</returns>
    public List<Vector2> Sample(Vector2 topLeft, Vector2 lowerRight, float? rejectionDistance,
        float minimumDistance, int pointsPerIteration)
    {
        var settings = new SampleSettings
        {
            TopLeft = topLeft, LowerRight = lowerRight,
            Dimensions = lowerRight - topLeft,
            Center = (topLeft + lowerRight) / 2,
            CellSize = minimumDistance / (float) Math.Sqrt(2),
            MinimumDistance = minimumDistance,
            RejectionSqDistance = rejectionDistance * rejectionDistance
        };

        settings.GridWidth = (int) (settings.Dimensions.X / settings.CellSize) + 1;
        settings.GridHeight = (int) (settings.Dimensions.Y / settings.CellSize) + 1;

        var state = new State
        {
            Grid = new Vector2?[settings.GridWidth, settings.GridHeight],
            ActivePoints = new List<Vector2>(),
            Points = new List<Vector2>()
        };

        AddFirstPoint(ref settings, ref state);

        while (state.ActivePoints.Count != 0)
        {
            var listIndex = _random.Next(state.ActivePoints.Count);

            var point = state.ActivePoints[listIndex];
            var found = false;

            for (var k = 0; k < pointsPerIteration; k++)
            {
                found |= AddNextPoint(point, ref settings, ref state);
            }

            if (!found)
                state.ActivePoints.RemoveAt(listIndex);
        }

        return state.Points;
    }

    private void AddFirstPoint(ref SampleSettings settings, ref State state)
    {
        var added = false;
        while (!added)
        {
            var d = _random.NextDouble();
            var xr = settings.TopLeft.X + settings.Dimensions.X * d;

            d = _random.NextDouble();
            var yr = settings.TopLeft.Y + settings.Dimensions.Y * d;

            var p = new Vector2((float) xr, (float) yr);
            if (settings.RejectionSqDistance != null &&
                (settings.Center - p).LengthSquared > settings.RejectionSqDistance)
                continue;
            added = true;

            var index = Denormalize(p, settings.TopLeft, settings.CellSize);

            state.Grid[(int) index.X, (int) index.Y] = p;

            state.ActivePoints.Add(p);
            state.Points.Add(p);
        }
    }

    private bool AddNextPoint(Vector2 point, ref SampleSettings settings, ref State state)
    {
        var found = false;
        var q = GenerateRandomAround(point, settings.MinimumDistance);

        if (q.X >= settings.TopLeft.X && q.X < settings.LowerRight.X &&
            q.Y > settings.TopLeft.Y && q.Y < settings.LowerRight.Y &&
            (settings.RejectionSqDistance == null ||
             (settings.Center - q).LengthSquared <= settings.RejectionSqDistance))
        {
            var qIndex = Denormalize(q, settings.TopLeft, settings.CellSize);
            var tooClose = false;

            for (var i = (int) Math.Max(0, qIndex.X - 2);
                 i < Math.Min(settings.GridWidth, qIndex.X + 3) && !tooClose;
                 i++)
            for (var j = (int) Math.Max(0, qIndex.Y - 2);
                 j < Math.Min(settings.GridHeight, qIndex.Y + 3) && !tooClose;
                 j++)
            {
                if (state.Grid[i, j].HasValue && (state.Grid[i, j]!.Value - q).Length < settings.MinimumDistance)
                    tooClose = true;
            }

            if (!tooClose)
            {
                found = true;
                state.ActivePoints.Add(q);
                state.Points.Add(q);
                state.Grid[(int) qIndex.X, (int) qIndex.Y] = q;
            }
        }

        return found;
    }

    private Vector2 GenerateRandomAround(Vector2 center, float minimumDistance)
    {
        var d = _random.NextDouble();
        var radius = minimumDistance + minimumDistance * d;

        d = _random.NextDouble();
        var angle = Math.PI * 2 * d;

        var newX = radius * Math.Sin(angle);
        var newY = radius * Math.Cos(angle);

        return new Vector2((float) (center.X + newX), (float) (center.Y + newY));
    }

    private static Vector2 Denormalize(Vector2 point, Vector2 origin, double cellSize)
    {
        return new Vector2((int) ((point.X - origin.X) / cellSize), (int) ((point.Y - origin.Y) / cellSize));
    }

    private struct State
    {
        public Vector2?[,] Grid;
        public List<Vector2> ActivePoints, Points;
    }

    private struct SampleSettings
    {
        public Vector2 TopLeft, LowerRight, Center;
        public Vector2 Dimensions;
        public float? RejectionSqDistance;
        public float MinimumDistance;
        public float CellSize;
        public int GridWidth, GridHeight;
    }
}



