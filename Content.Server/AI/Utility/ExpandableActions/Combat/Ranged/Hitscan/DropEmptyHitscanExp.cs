using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Hitscan
{
    public class DropEmptyHitscanExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<InventoryState>().GetValue())
            {
                if (entity.HasComponent<HitscanWeaponComponent>())
                {
                    yield return new DropEmptyHitscan(owner, entity, Bonus);
                }
            }
        }
    }
}
