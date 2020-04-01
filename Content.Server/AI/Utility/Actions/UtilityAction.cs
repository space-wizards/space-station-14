using System;
using System.Collections.Generic;
using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.AI.Utility.Actions
{
    /// <summary>
    /// The same DSE can be used across multiple actions.
    /// </summary>
    public abstract class UtilityAction : IAiUtility
    {
        /// <summary>
        /// If we're trying to find a new action can we replace a currently running one with one of the same type.
        /// e.g. If you're already wandering you don't want to replace it with a different wander.
        /// </summary>
        public virtual bool CanOverride => false;

        public virtual BonusWeight Bonus { get; protected set; } = BonusWeight.Invalid;

        protected IEntity Owner { get; }

        /// <summary>
        /// All the considerations are multiplied together to get the final score; a consideration of 0.0 means the action is not possible.
        /// Ideally you put anything that's easy to assess and can cause an early-out first just so the rest aren't evaluated.
        /// </summary>
        protected abstract Consideration[] Considerations { get; }

        /// <summary>
        /// To keep the operators simple we can chain them together here, e.g. move to can be chained with other operators.
        /// </summary>
        public Queue<IOperator> ActionOperators { get; protected set; }

        /// <summary>
        /// Sometimes we may need to set the target for an action or the likes.
        /// This is mainly useful for expandable states so each one can have a separate target.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void UpdateBlackboard(Blackboard context) {}

        protected UtilityAction(IEntity owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// If this action is chosen then setup the operators to run. This also allows for operators to be reset.
        /// </summary>
        public abstract void SetupOperators(Blackboard context);

        // Call the task's operator with Execute and get the outcome
        public Outcome Execute(float frameTime)
        {
            if (!ActionOperators.TryPeek(out var op))
            {
                return Outcome.Success;
            }

            var outcome = op.Execute(frameTime);

            if (outcome == Outcome.Success)
            {
                ActionOperators.Dequeue();
            }

            return outcome;
        }

        /// <summary>
        /// AKA the Decision Score Evaluator (DSE)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bonus"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public float GetScore(Blackboard context, float min)
        {
            UpdateBlackboard(context);
            DebugTools.Assert(Considerations.Length > 0);
            // I used the IAUS video although I did have some confusion on how to structure it overall
            // as some of the slides seemed contradictory

            // Ideally we should early-out each action as cheaply as possible if it's not valid

            // We also need some way to tell if the action isn't going to
            // have a better score than the current action (if applicable) and early-out that way as well.

            // 23:00 Building a better centaur
            var bonusValue = (float) Bonus;
            var finalScore = 1.0f;
            var minThreshold = min / bonusValue;
            var modificationFactor = 1 - (1.0f / Considerations.Length);
            // See 10:09 for this and the adjustments

            foreach (var consideration in Considerations)
            {
                var score = consideration.GetScore(context); // TODO: consideration parameters, e.g. range limits?
                var makeUpValue = (1.0f - score) * modificationFactor;
                var adjustedScore = score + (makeUpValue * score);
                var response = consideration.ComputeResponseCurve(adjustedScore);

                finalScore *= Math.Clamp(response, 0.0f, 1.0f);

                DebugTools.Assert(!float.IsNaN(response));

                // The score can only ever go down from each consideration so if we're below minimum no point continuing.
                if (0.0f >= finalScore || finalScore < minThreshold) {
                    return 0.0f;
                }
            }

            DebugTools.Assert(finalScore <= 1.0f);

            return finalScore * bonusValue;
        }
    }

    /// <summary>
    /// This is used to sort actions; if there's a top-tier action available we won't bother checking the lower tiers.
    /// Cast to a float by the utility actions
    /// Threshold doesn't necessarily mean we'll do an action at a higher threshold;
    /// if it's really un-optimal (i.e. low score) then we'll also check lower tiers
    /// </summary>
    public enum BonusWeight : ushort
    {
        Invalid = 0,
        // Idle should just be flavor-stuff if we reeaaalllyyy have nothing to do
        // TODO: Look at dividing these by 10 for more granularity.
        // Could also just use the floats as bonuses as straight up but it'll be nicer to have an enum as a guide
        Idle = 1,
        // Use a computer, bark at someone, whatever
        Normal = 5,
        // Hunger / thirst etc.
        Needs = 10,
        // Picking up weapons etc.
        CombatPrep = 20,
        Combat = 30,
        // Avoid that grenade
        Danger = 50,
    }
}
