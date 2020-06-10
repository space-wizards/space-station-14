using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Ballistic
{
    public sealed class DropEmptyBallisticExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<InventoryState>().GetValue())
            {
                if (entity.HasComponent<BallisticMagazineWeaponComponent>())
                {
                    yield return new DropEmptyBallistic(owner, entity, Bonus);
                }
            }
        }
    }
}
