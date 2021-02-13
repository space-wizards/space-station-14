using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Nutrition;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class ThirstBehaviorSet : BehaviorSet
    {
        public ThirstBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new PickUpNearbyDrinkExp(),
                new UseDrinkInInventoryExp(),
            };
        }
    }
}
