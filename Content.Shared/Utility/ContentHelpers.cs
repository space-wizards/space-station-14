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
                return (int)Math.Ceiling(preround);
            }
            else
            {
                return (int)Math.Floor(preround);
            }
        }

        /// <summary>
        /// Finds the closest level out of a list of levels to the given value, in terms of their difference (|a-b|).
        /// </summary>
        /// <example>
        /// You have a scale from 0 to 100 with points at 0, 25, 50, 75 and 100, and a value between 0 and 100.
        /// You want to find the nearest point to the value.
        /// </example>
        /// <param name="value">Value to round.</param>
        /// <param name="levels">Levels to round to.</param>
        /// <returns>The closest level to the given value.</returns>
        public static double RoundToClosest(double value, params double[] levels)
        {
            var nearestDiff = Math.Abs(value - levels[0]);
            var nearest = levels[0];
            for (var i = 1; i < levels.Length; i++)
            {
                var diff = Math.Abs(value - levels[i]);
                if (diff < nearestDiff)
                {
                    nearestDiff = diff;
                    nearest = levels[i];
                }
            }
            return nearest;
        }

        /// <summary>
        /// Finds the closest level out of a list of levels to the given value, in terms of their difference (|a-b|).
        /// </summary>
        /// <example>
        /// You have a scale from 0 to 100 with points at 0, 25, 50, 75 and 100, and a value between 0 and 100.
        /// You want to find the nearest point to the value.
        /// </example>
        /// <param name="value">Value to round.</param>
        /// <param name="levels">Levels to round to.</param>
        /// <returns>The closest level to the given value.</returns>
        public static int RoundToClosest(int value, params int[] levels)
        {
            var nearestDiff = Math.Abs(value - levels[0]);
            var nearest = levels[0];
            for (var i = 1; i < levels.Length; i++)
            {
                var diff = Math.Abs(value - levels[i]);
                if (diff < nearestDiff)
                {
                    nearestDiff = diff;
                    nearest = levels[i];
                }
            }
            return nearest;
        }
    }
}
