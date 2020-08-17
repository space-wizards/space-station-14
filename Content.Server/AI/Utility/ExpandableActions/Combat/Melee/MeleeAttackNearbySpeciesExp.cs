using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Melee;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Mobs;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Melee
{
    public sealed class MeleeAttackNearbySpeciesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyBodiesState>().GetValue())
            {
                yield return new MeleeWeaponAttackEntity(owner, entity, Bonus);
            }
        }
    }
}
