using System;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;

namespace Content.Server.AI.Utility.Considerations
{
    public abstract class Consideration
    {
        protected abstract float GetScore(Blackboard context);

        private float GetAdjustedScore(Blackboard context)
        {
            var score = GetScore(context);
            var considerationsCount = context.GetState<ConsiderationState>().GetValue();
            var modificationFactor = 1.0f - 1.0f / considerationsCount;
            var makeUpValue = (1.0f - score) * modificationFactor;
            var adjustedScore = score + makeUpValue * score;
            return Math.Clamp(adjustedScore, 0.0f, 1.0f);
        }

        public Func<float> BoolCurve(Blackboard context)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return adjustedScore == 1.0f ? 1.0f : 0.0f;
            }

            return Result;
        }

        public Func<float> InverseBoolCurve(Blackboard context)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return adjustedScore == 1.0f ? 0.0f : 1.0f;
            }

            return Result;
        }

        public Func<float> LogisticCurve(Blackboard context, float slope, float exponent, float yOffset, float xOffset)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return Math.Clamp(exponent * (1 / (1 + (float) Math.Pow(Math.Log(1000) * slope, -1 * adjustedScore + xOffset))) + yOffset, 0.0f, 1.0f);
            }

            return Result;
        }

        public Func<float> QuadraticCurve(Blackboard context, float slope, float exponent, float yOffset, float xOffset)
        {
            float Result()
            {
                var adjustedScore = GetAdjustedScore(context);
                return Math.Clamp(slope * (float) Math.Pow(adjustedScore - xOffset, exponent) + yOffset, 0.0f, 1.0f);
            }

            return Result;
        }
    }
}
