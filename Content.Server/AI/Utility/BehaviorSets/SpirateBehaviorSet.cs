using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.ExpandableActions.Combat.Melee;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class SpirateBehaviorSet : BehaviorSet
    {
        public SpirateBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                // TODO: Reload Ballistic
                // TODO: Ideally long-term we should just store the weapons in backpack
                new EquipMeleeExp(),
                new PickUpMeleeWeaponExp(),
                new MeleeAttackNearbyExp(),
            };
        }
    }
}
