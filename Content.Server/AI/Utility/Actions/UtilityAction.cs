using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Utility;
using Robust.Shared.GameObjects;
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

        /// <summary>
        /// This is used to sort actions; if there's a top-tier action available we won't bother checking the lower tiers.
        /// Threshold doesn't necessarily mean we'll do an action at a higher threshold;
        /// if it's really un-optimal (i.e. low score) then we'll also check lower tiers
        /// </summary>
        public virtual float Bonus { get; set; } = IdleBonus;
        // For GW2 they had the bonuses close together but IMO it feels better when they're more like discrete tiers.

        // These are just baselines to make mass-updates easier; actions can do whatever
        // e.g. if you want shooting a gun to be considered before picking up a gun you could + 1.0f it or w/e
        public const float IdleBonus = 1.0f;
        public const float NormalBonus = 5.0f;
        public const float NeedsBonus = 10.0f;
        public const float CombatPrepBonus = 20.0f;
        public const float CombatBonus = 30.0f;
        public const float DangerBonus = 50.0f;

        public EntityUid Owner { get; set; }

        /// <summary>
        /// All the considerations are multiplied together to get the final score; a consideration of 0.0 means the action is not possible.
        /// Ideally you put anything that's easy to assess and can cause an early-out first just so the rest aren't evaluated.
        /// </summary>
        /// Uses Func<float> as you don't want to eval the later considerations unless necessary, but we also need the total count
        /// so can't use IEnumerable
        protected abstract IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context);

        /// <summary>
        /// To keep the operators simple we can chain them together here, e.g. move to can be chained with other operators.
        /// </summary>
        public Queue<AiOperator> ActionOperators { get; protected set; }

        /// <summary>
        /// Sometimes we may need to set the target for an action or the likes.
        /// This is mainly useful for expandable states so each one can have a separate target.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void UpdateBlackboard(Blackboard context) {}

        // Needs to be able to be instantiated without args via typefactory.
        public UtilityAction()
        {
            Owner = default!;
            ActionOperators = default!;
        }

        public virtual void Shutdown() {}

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

            op.Startup();
            var outcome = op.Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    op.Shutdown(outcome);
                    ActionOperators.Dequeue();
                    break;
                case Outcome.Continuing:
                    break;
                case Outcome.Failed:
                    op.Shutdown(outcome);
                    ActionOperators.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return outcome;
        }

        /// <summary>
        /// AKA the Decision Score Evaluator (DSE)
        /// This is where the magic happens
        /// </summary>
        /// <param name="context"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public float GetScore(Blackboard context, float min)
        {
            UpdateBlackboard(context);
            var considerations = GetConsiderations(context);
            DebugTools.Assert(considerations.Count > 0);

            // Overall structure is based on Building a better centaur
            // Ideally we should early-out each action as cheaply as possible if it's not valid, thus
            // the finalScore can only go down over time.

            var finalScore = 1.0f;
            var minThreshold = min / Bonus;
            context.GetState<ConsiderationState>().SetValue(considerations.Count);

            foreach (var consideration in considerations)
            {
                var score = consideration.Invoke();
                finalScore *= score;
                DebugTools.Assert(!float.IsNaN(score));

                // The score can only ever go down from each consideration so if we're below minimum no point continuing.
                if (0.0f >= finalScore || finalScore < minThreshold) {
                    return 0.0f;
                }
            }

            DebugTools.Assert(finalScore <= 1.0f);

            return finalScore * Bonus;
        }
    }
}
