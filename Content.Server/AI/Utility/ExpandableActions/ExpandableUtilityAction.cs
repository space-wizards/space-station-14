using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.WorldState;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.ExpandableActions
{
    /// <summary>
    /// Expands into multiple separate utility actions for consideration, e.g. 5 nearby weapons 5 different actions
    /// Ideally you would use the cached states for this
    /// </summary>
    public abstract class ExpandableUtilityAction : IAiUtility
    {
        public EntityUid Owner { get; set; } = default!;

        public abstract float Bonus { get; }

        /// <summary>
        /// No point expanding nodes if none of them can ever be valid.
        /// Fails if any of the common considerations is 0.0f (i.e. invalid)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool IsValid(Blackboard context)
        {
            foreach (var con in GetCommonConsiderations(context))
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (con.Invoke() == 0.0f) return false;
            }

            return true;
        }

        /// <summary>
        /// Called by IsValid to try and early-out the expandable action.
        /// No point going through all nearby clothes if we can't fit it in a slot.
        /// </summary>
        /// Similar to HTN's compound tasks where they can have overall conditions that have to be met before the actions are considered.
        /// Ideally any binary early-outs that are common to all expanded actions would be checked once, e.g. a boolean free hand check
        /// Use this if you want to optimise the expandable further.
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            yield break;
        }

        // e.g. you may have a "PickupFood" action for all nearby food sources if you have the "Hungry" BehaviorSet.
        public abstract IEnumerable<UtilityAction> GetActions(Blackboard context);
    }
}
