using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Nutrition.Food;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Nutrition
{
    public sealed class UseFoodInInventoryExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<InventoryState>().GetValue())
            {
                yield return new UseFoodInInventory(owner, entity, Bonus);
            }
        }
    }
}
