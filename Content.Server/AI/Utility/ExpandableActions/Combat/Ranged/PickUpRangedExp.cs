using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.Utils;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat.Nearby;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged
{
    public sealed class PickUpRangedExp : ExpandableUtilityAction
    {
        public override BonusWeight Bonus => BonusWeight.CombatPrep;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.TryGetComponent(out AiControllerComponent controller))
            {
                throw new InvalidOperationException();
            }

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
