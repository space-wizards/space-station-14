using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class InventoryBehaviorSet : BehaviorSet
    {
        public InventoryBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {

            };
        }
    }
}
