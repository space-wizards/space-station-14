using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Clothing.Gloves;
using Content.Server.AI.Utility.ExpandableActions.Clothing.Head;
using Content.Server.AI.Utility.ExpandableActions.Clothing.OuterClothing;
using Content.Server.AI.Utility.ExpandableActions.Clothing.Shoes;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class ClothingBehaviorSet : BehaviorSet
    {
        public ClothingBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new EquipAnyHeadExp(),
                new EquipAnyOuterClothingExp(),
                new EquipAnyGlovesExp(),
                new EquipAnyShoesExp(),
                new PickUpAnyNearbyHeadExp(),
                new PickUpAnyNearbyOuterClothingExp(),
                new PickUpAnyNearbyGlovesExp(),
                new PickUpAnyNearbyShoesExp(), 
            };
        }
    }
}
