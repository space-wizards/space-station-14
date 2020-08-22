using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility.ExpandableActions
{
    /// <summary>
    /// Expands into multiple separate utility actions for consideration, e.g. 5 nearby weapons 5 different actions
    /// Ideally you would use the cached states for this
    /// </summary>
    public abstract class ExpandableUtilityAction : IAiUtility
    {
        public abstract float Bonus { get; }

        // e.g. you may have a "PickupFood" action for all nearby food sources if you have the "Hungry" BehaviorSet.
        public abstract IEnumerable<UtilityAction> GetActions(Blackboard context);
    }
}
