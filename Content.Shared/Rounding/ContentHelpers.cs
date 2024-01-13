namespace Content.Shared.Rounding
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
            if (toOne < threshold || levels <= 2)
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
        /// <exception cref="ArgumentException">If level is 1 or less</exception>
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

            return (int) Math.Round(actual / max * levels, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Basically helper for when you need to choose 0..N-1 element based on what
        /// percentage does actual/max takes.
        /// Example:
        /// We have a stack of 30 <paramref name="max"/> elements.
        /// When <paramref name="actual"/> is:
        /// - 0..9 we return 0.
        /// - 10..19 we return 1.
        /// - 20..30 we return 2.
        ///
        /// Useful when selecting N sprites for display in stacks, etc.
        /// </summary>
        /// <param name="actual">How many out of max elements are there</param>
        /// <param name="max"></param>
        /// <param name="levels"></param>
        /// <returns>The </returns>
        /// <exception cref="ArgumentException">if level is one or less</exception>
        public static int RoundToEqualLevels(double actual, double max, int levels)
        {
            if (levels <= 1)
            {
                throw new ArgumentException("Levels must be greater than 1.", nameof(levels));
            }

            if (actual >= max)
            {
                return levels - 1;
            }

            if (actual <= 0)
            {
                return 0;
            }

            return (int) Math.Round(actual / max * levels, MidpointRounding.ToZero);
        }
    }
}
