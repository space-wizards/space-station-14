using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Clothing.Gloves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Clothing.Gloves
{
    /// <summary>
    /// Equip any head item currently in our inventory
    /// </summary>
    public sealed class EquipAnyGlovesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NormalBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (entity.TryGetComponent(out ClothingComponent clothing) &&
                    (clothing.SlotFlags & EquipmentSlotDefines.SlotFlags.GLOVES) != 0)
                {
                    yield return new EquipGloves(owner, entity, Bonus);
                }
            }
        }
    }
}
