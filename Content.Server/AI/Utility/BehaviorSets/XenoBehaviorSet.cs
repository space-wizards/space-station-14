using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Combat.Melee;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class XenoBehaviorSet : BehaviorSet
    {
        public XenoBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new XenoMeleeAttackNearbyPlayerExp(),
            };
        }
    }
}