using System;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Content.Server.AI.Utility.Considerations
{
    public abstract class Consideration
    {
        protected abstract float GetScore(Blackboard context);

        private float GetAdjustedScore(Blackboard context)
        {
            var score = GetScore(context);
            /*
            * Now using the geometric mean
            * for n scores you take the n-th root of the scores multiplied
            * e.g. a, b, c scores you take Math.Pow(a * b * c, 1/3)
            * To get the ACTUAL geometric mean at any one stage you'd need to divide by the running consideration count
            * however, the downside to this is it will fluctuate up and down over time.
            * For our purposes if we go below the minimum threshold we want to cut it off, thus we take a
            * "running geometric mean" which can only ever go down (and by the final value will equal the actual geometric mean).
            */

            // Previously we used a makeupvalue method although the geometric mean is less punishing for more considerations
            var considerationsCount = context.GetState<ConsiderationState>().GetValue();
            var adjustedScore = MathF.Pow(score, 1 / (float) considerationsCount);
            return MathHelper.Clamp(adjustedScore, 0.0f, 1.0f);
        }

        [Pure]
        private static float BoolCurve(float x)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return x > 0.0f ? 1.0f : 0.0f;
        }

        public Func<float> BoolCurve(Blackboard context)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return BoolCurve(adjustedScore);
            }

            return Result;
        }

        [Pure]
        private static float InverseBoolCurve(float x)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return x == 0.0f ? 1.0f : 0.0f;
        }

        public Func<float> InverseBoolCurve(Blackboard context)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return InverseBoolCurve(adjustedScore);
            }

            return Result;
        }

        [Pure]
        private static float LogisticCurve(float x, float slope, float exponent, float yOffset, float xOffset)
        {
            return MathHelper.Clamp(
                exponent * (1 / (1 + (float) Math.Pow(Math.Log(1000) * slope, -1 * x + xOffset))) + yOffset, 0.0f, 1.0f);
        }

        public Func<float> LogisticCurve(Blackboard context, float slope, float exponent, float yOffset, float xOffset)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return LogisticCurve(adjustedScore, slope, exponent, yOffset, xOffset);
            }

            return Result;
        }

        [Pure]
        private static float QuadraticCurve(float x, float slope, float exponent, float yOffset, float xOffset)
        {
            return MathHelper.Clamp(slope * (float) Math.Pow(x - xOffset, exponent) + yOffset, 0.0f, 1.0f);
        }

        public Func<float> QuadraticCurve(Blackboard context, float slope, float exponent, float yOffset, float xOffset)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return QuadraticCurve(adjustedScore, slope, exponent, yOffset, xOffset);
            }

            return Result;
        }

        /// <summary>
        /// For any curves that are re-used across actions so you only need to update it once.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="preset"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Func<float> PresetCurve(Blackboard context, PresetCurve preset)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);

                switch (preset)
                {
                    case Considerations.PresetCurve.Distance:
                        return QuadraticCurve(adjustedScore, -1.0f, 1.0f, 1.0f, 0.02f);
                    case Considerations.PresetCurve.Nutrition:
                        return QuadraticCurve(adjustedScore, 2.0f, 1.0f, -1.0f, -0.2f);
                    case Considerations.PresetCurve.TargetHealth:
                        return QuadraticCurve(adjustedScore, 1.0f, 0.4f, 0.0f, -0.02f);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
                }
            }

            return Result;
        }
    }

    /// <summary>
    /// Preset response curves for considerations
    /// </summary>
    public enum PresetCurve
    {
        Distance,
        Nutrition,
        TargetHealth,
    }
}
