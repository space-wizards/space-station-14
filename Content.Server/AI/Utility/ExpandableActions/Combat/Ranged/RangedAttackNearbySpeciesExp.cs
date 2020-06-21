using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Mobs;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged
{
    public sealed class RangedAttackNearbySpeciesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            yield break;
        }
    }
}
