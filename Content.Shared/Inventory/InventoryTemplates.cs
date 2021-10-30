using System.Collections.Generic;
using JetBrains.Annotations;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Inventory
{
    public abstract class Inventory
    {
        public abstract string InterfaceControllerTypeName { get; }

        public abstract IReadOnlyList<Slots> SlotMasks { get; }

        /// <summary>
        ///     Gets the drawing order of a slot.
        /// </summary>
        /// <returns>
        ///     An int that can be used for sorting relative to other drawing orders.
        ///     The value returned does not mean anything else.
        /// </returns>
        public abstract int SlotDrawingOrder(Slots slot);
    }

    // Dynamically created by SharedInventoryComponent.
    [UsedImplicitly]
    public class HumanInventory : Inventory
    {
        public override string InterfaceControllerTypeName => "HumanInventoryInterfaceController";

        private static readonly Dictionary<Slots, int> _slotDrawingOrder = new()
        {
            {Slots.POCKET1, 13},
            {Slots.POCKET2, 12},
            {Slots.HEAD, 11},
            {Slots.MASK, 10},
            {Slots.EARS, 9},
            {Slots.NECK, 8},
            {Slots.BACKPACK, 7},
            {Slots.EYES, 6},
            {Slots.OUTERCLOTHING, 5},
            {Slots.BELT, 4},
            {Slots.GLOVES, 3},
            {Slots.SHOES, 2},
            {Slots.IDCARD, 1},
            {Slots.INNERCLOTHING, 0}
        };

        public override IReadOnlyList<Slots> SlotMasks { get; } = new List<Slots>()
        {
            Slots.EYES, Slots.HEAD, Slots.EARS,
            Slots.OUTERCLOTHING, Slots.MASK, Slots.INNERCLOTHING,
            Slots.BACKPACK, Slots.BELT, Slots.GLOVES,
            Slots.NONE, Slots.SHOES, Slots.IDCARD, Slots.POCKET1, Slots.POCKET2,
            Slots.NECK
        };

        public override int SlotDrawingOrder(Slots slot)
        {
            return _slotDrawingOrder.TryGetValue(slot, out var val) ? val : 0;
        }
    }
}
