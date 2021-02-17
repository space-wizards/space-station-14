using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Nutrition;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class HungerBehaviorSet : BehaviorSet
    {
        public HungerBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new PickUpNearbyFoodExp(),
                new UseFoodInInventoryExp(),
            };
        }
    }
}
