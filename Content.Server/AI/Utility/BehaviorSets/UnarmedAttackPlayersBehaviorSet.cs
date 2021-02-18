using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Combat.Melee;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class UnarmedAttackPlayersBehaviorSet : BehaviorSet
    {
        public UnarmedAttackPlayersBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new UnarmedAttackNearbyPlayerExp(), 
            };
        }
    }
}