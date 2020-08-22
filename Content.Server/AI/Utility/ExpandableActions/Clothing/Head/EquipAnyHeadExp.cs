using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Clothing.Head;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Clothing.Head
{
    /// <summary>
    /// Equip any head item currently in our inventory
    /// </summary>
    public sealed class EquipAnyHeadExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NormalBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (entity.TryGetComponent(out ClothingComponent clothing) &&
                    (clothing.SlotFlags & EquipmentSlotDefines.SlotFlags.HEAD) != 0)
                {
                    yield return new EquipHead(owner, entity, Bonus);
                }
            }
        }
    }
}
