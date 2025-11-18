using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Worldgen.Tools;

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
    /// <param name="minimumDistance">Minimum distance between points. Must be above 0!</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>An enumerator of points</returns>
    public SampleEnumerator SampleCircle(Vector2 center, float radius, float minimumDistance,
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
    /// <param name="minimumDistance">Minimum distance between points. Must be above 0!</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>An enumerator of points</returns>
    public SampleEnumerator SampleRectangle(Vector2 topLeft, Vector2 lowerRight, float minimumDistance,
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
    /// <param name="minimumDistance">Minimum distance between points. Must be above 0!</param>
    /// <param name="pointsPerIteration">The number of points placed per iteration of the algorithm</param>
    /// <returns>An enumerator of points</returns>
    public SampleEnumerator Sample(Vector2 topLeft, Vector2 lowerRight, float? rejectionDistance,
        float minimumDistance, int pointsPerIteration)
    {
        // This still doesn't guard against dangerously low but non-zero distances, but this will do for now.
        DebugTools.Assert(minimumDistance > 0, "Minimum distance must be above 0, or else an infinite number of points would be generated.");

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
            ActivePoints = new List<Vector2>()
        };

        return new SampleEnumerator(this, state, settings, pointsPerIteration);
    }

    private Vector2 AddFirstPoint(ref SampleSettings settings, ref State state)
    {
        while (true)
        {
            var d = _random.NextDouble();
            var xr = settings.TopLeft.X + settings.Dimensions.X * d;

            d = _random.NextDouble();
            var yr = settings.TopLeft.Y + settings.Dimensions.Y * d;

            var p = new Vector2((float) xr, (float) yr);
            if (settings.RejectionSqDistance != null &&
                (settings.Center - p).LengthSquared() > settings.RejectionSqDistance)
                continue;

            var index = Denormalize(p, settings.TopLeft, settings.CellSize);

            state.Grid[(int) index.X, (int) index.Y] = p;

            state.ActivePoints.Add(p);
            return p;
        }
    }

    private Vector2? AddNextPoint(Vector2 point, ref SampleSettings settings, ref State state)
    {
        var q = GenerateRandomAround(point, settings.MinimumDistance);

        if (q.X >= settings.TopLeft.X && q.X < settings.LowerRight.X &&
            q.Y > settings.TopLeft.Y && q.Y < settings.LowerRight.Y &&
            (settings.RejectionSqDistance == null ||
             (settings.Center - q).LengthSquared() <= settings.RejectionSqDistance))
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
                if (state.Grid[i, j].HasValue && (state.Grid[i, j]!.Value - q).Length() < settings.MinimumDistance)
                    tooClose = true;
            }

            if (!tooClose)
            {
                state.ActivePoints.Add(q);
                state.Grid[(int) qIndex.X, (int) qIndex.Y] = q;
                return q;
            }
        }

        return null;
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

    public struct SampleEnumerator
    {
        private PoissonDiskSampler _pds;
        private State _state;
        private SampleSettings _settings;
        // These variables make up the state machine.
        private bool _returnedFirstPoint;
        private int _pointsPerIteration;
        private int _iterationListIndex;
        private bool _iterationFound;
        private int _iterationPosition;

        // This has internal access because C# nested type access is being weird.
        internal SampleEnumerator(PoissonDiskSampler pds, State state, SampleSettings settings, int ppi)
        {
            _pds = pds;
            _state = state;
            _settings = settings;
            _pointsPerIteration = ppi;
        }

        public bool MoveNext([NotNullWhen(true)] out Vector2? point)
        {
            // First point is chosen via a very particular method.
            if (!_returnedFirstPoint)
            {
                _returnedFirstPoint = true;
                point = _pds.AddFirstPoint(ref _settings, ref _state);
                return true;
            }

            // Remaining points have to be fed out carefully.
            // We can be interrupted (by a successful point) mid-stream.
            while (_state.ActivePoints.Count != 0)
            {
                if (_iterationPosition == 0)
                {
                    // First point of iteration.
                    _iterationListIndex = _pds._random.Next(_state.ActivePoints.Count);
                    _iterationFound = false;
                }

                var basePoint = _state.ActivePoints[_iterationListIndex];

                point = _pds.AddNextPoint(basePoint, ref _settings, ref _state);

                // Set this now, return later after processing is complete.
                _iterationFound |= point != null;

                // Iteration loop advance.
                _iterationPosition++;
                if (_iterationPosition == _pointsPerIteration)
                {
                    // Reached end of this iteration.
                    _iterationPosition = 0;
                    if (!_iterationFound)
                        _state.ActivePoints.RemoveAt(_iterationListIndex);
                }

                if (point != null)
                    return true;
            }
            point = null;
            return false;
        }
    }

    internal struct State
    {
        public Vector2?[,] Grid;
        public List<Vector2> ActivePoints;
    }

    internal struct SampleSettings
    {
        public Vector2 TopLeft, LowerRight, Center;
        public Vector2 Dimensions;
        public float? RejectionSqDistance;
        public float MinimumDistance;
        public float CellSize;
        public int GridWidth, GridHeight;
    }
}



