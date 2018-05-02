using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.GameObjects
{
    public abstract class Inventory
    {
        abstract public int Columns { get; }

        abstract public List<Slots> SlotMasks { get; }
    }

    public class HumanInventory : Inventory
    {
        public override int Columns => 3;

        public override List<Slots> SlotMasks => new List<Slots>()
            {
                Slots.EYES, Slots.HEAD, Slots.EARS,
                Slots.OUTERCLOTHING, Slots.MASK, Slots.INNERCLOTHING,
                Slots.BACKPACK, Slots.BELT, Slots.GLOVES,
                Slots.NONE, Slots.SHOES, Slots.IDCARD
            };
    }
}
