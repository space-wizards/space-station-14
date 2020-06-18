using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Ballistic
{
    public sealed class EquipBallisticExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<InventoryState>().GetValue())
            {
                yield return new EquipBallistic(owner, entity, Bonus);
            }
        }
    }
}
