using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    /// <summary>
    /// AKA DecisionMaker in IAUS. Just a group of actions that can be dynamically added or taken away from an AI.
    /// </summary>
    public abstract class BehaviorSet
    {
        protected IEntity Owner;

        public BehaviorSet(IEntity owner)
        {
            Owner = owner;
        }

        public IEnumerable<IAiUtility> Actions { get; protected set; }
    }
}
