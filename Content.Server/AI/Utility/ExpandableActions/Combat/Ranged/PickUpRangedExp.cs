using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat.Nearby;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged
{
    public sealed class PickUpRangedExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyRangedWeapons>().GetValue())
            {
                if (entity.HasComponent<HitscanWeaponComponent>())
                {
                    yield return new PickUpHitscanWeapon(owner, entity, Bonus);
                }

                if (entity.HasComponent<BallisticMagazineWeaponComponent>())
                {
                    yield return new PickUpBallisticMagWeapon(owner, entity, Bonus);
                }
            }
        }
    }
}
