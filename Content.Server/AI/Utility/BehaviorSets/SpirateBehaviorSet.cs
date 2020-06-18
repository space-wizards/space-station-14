using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.Utility.ExpandableActions.Combat;
using Content.Server.AI.Utility.ExpandableActions.Combat.Melee;
using Content.Server.AI.Utility.ExpandableActions.Combat.Ranged;
using Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Hitscan;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class SpirateBehaviorSet : BehaviorSet
    {
        public SpirateBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new PickUpRangedExp(),
                // TODO: Reload Ballistic
                new DropEmptyBallisticExp(),
                // TODO: Ideally long-term we should just store the weapons in backpack
                new DropEmptyHitscanExp(),
                new EquipMeleeExp(),
                new EquipBallisticExp(),
                new EquipHitscanExp(),
                new PickUpHitscanFromChargersExp(),
                new ChargeEquippedHitscanExp(),
                new RangedAttackNearbySpeciesExp(),
                new PickUpMeleeWeaponExp(),
                new MeleeAttackNearbySpeciesExp(),
            };
        }
    }
}
