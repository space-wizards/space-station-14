using System;

namespace Content.Shared.Utility
{
    public static class ContentHelpers
    {
        /// <summary>
        ///     Assigns the value <paramref name="actual" /> going from 0 to <paramref name="max" />
        ///     such that it is divided into a set of (amount <paramref name="levels" />) "levels".
        ///     Rounding is performed to the "middle" so that the highest and lowest levels are only assigned
        ///     if <paramref name="actual" /> is exactly <paramref name="max" /> or 0.
        /// </summary>
        /// <example>
        ///     Say you have a progress bar going from 0 -> 100 inclusive and you want to map this to 6 sprite states (0, 4 intermediates and full).
        ///     This method allows you to easily map this progress bar to the sprite states.
        /// </example>
        /// <param name="levels">The amount of levels to subdivide into.</param>
        /// <returns>An integer from 0 to <paramref name="levels" />-1.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if levels is less than 1.
        /// </exception>
        public static int RoundToLevels(double actual, double max, int levels)
        {
            if (levels <= 0)
            {
                throw new ArgumentException("Levels must be greater than 0.", nameof(levels));
            }

            if (actual >= max)
            {
                return levels - 1;
            }

            if (actual <= 0)
            {
                return 0;
            }

            var toOne = actual / max;
            double threshold;
            if (levels % 2 == 0)
            {
                // Basically, if we have an even count of levels, there's no exact "mid point".
                // Thus, I nominate the first one below the 50% mark.
                threshold = ((levels / 2f) - 1) / (levels - 1);
            }
            else
            {
                threshold = 0.5f;
            }

            var preround = toOne * (levels - 1);
            if (toOne <= threshold || levels <= 2)
            {
                return (int) Math.Ceiling(preround);
            }
            else
            {
                return (int) Math.Floor(preround);
            }
        }

        /// <summary>
        /// Returns the segment <paramref name="actual"/> lies on on a decimal scale from 0 to <paramref name="max"/> divided into
        /// <paramref name="levels"/> sections. In less mathematical terms, same as <see cref="RoundToLevels"/>
        /// except <paramref name="actual"/> is rounded to the nearest matching level instead of 0 and the highest level being
        /// precisely 0 and max and no other value.
        /// </summary>
        /// <example>
        /// You have a 5-segment progress bar used to display a percentile value.
        /// You want the display to match the percentile value as accurately as possible, so that eg.
        /// 95% is rounded up to 5, 89.99% is rounded down to 4, 15% is rounded up to 1 and 5% is rounded down
        /// to 0, in terms of number of segments lit.
        /// In this case you would use <code>RoundToNearestLevels(value, max, 5)</code>
        /// </example>
        /// <param name="actual">The point to be rounded to the nearest level.</param>
        /// <param name="max">The maximum value of the scale.</param>
        /// <param name="levels">Number of segments the scale is subdivided into.</param>
        /// <returns>The segment <paramref name="actual"/> lies on.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static int RoundToNearestLevels(double actual, double max, int levels)
        {
            if (levels <= 1)
            {
                throw new ArgumentException("Levels must be greater than 1.", nameof(levels));
            }

            if (actual >= max)
            {
                return levels;
            }

            if (actual <= 0)
            {
                return 0;
            }

            double step = max / levels;

            int nearest = 0;
            double nearestDiff = actual;
            for (var i = 1; i <= levels; i++)
            {
                var diff = Math.Abs(actual - i * step);
                if (diff < nearestDiff)
                {
                    nearestDiff = diff;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns the index of an array of <paramref name="size"/> which roughly corresponds to where <paramref name="actual"/> lies
        /// on on a decimal scale from 0 to <paramref name="max"/>.
        /// </summary>
        /// <example>
        /// Imagine you have an array of 3 images [icon-0, icon-1, icon-2] and stack of 100 wires.
        /// You want 0-33 wires to correspond to icon-0. And 34-67 wires to correspond to icon-1, etc.
        /// In this case you would use <code>RoundToNearestIndex(actual, 100, 3)</code>
        /// </example>
        /// <param name="actual">The point to be translated into index.</param>
        /// <param name="max">The maximum value of the scale</param>
        /// <param name="size">The size of array you want to map to.</param>
        /// <returns>An integer from 0 to <paramref name="size" />-1.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if size is less than 1.
        ///     Thrown if max is 0 or less.
        /// </exception>
        public static int RoundToNearestIndex(double actual, double max, int size)
        {
            if (size <= 1)
            {
                throw new ArgumentException("Size must be greater than 1.", nameof(size));
            }

            if (max <= 0)
            {
                throw new ArgumentException("Max must be greater than 0.", nameof(size));
            }

            if (actual >= max)
            {
                return size - 1;
            }

            var percentile = actual / max;

            return (int) Math.Round(percentile * (size - 1), MidpointRounding.AwayFromZero);
        }
    }
}
