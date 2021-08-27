using Robust.Shared.GameObjects;

namespace Content.Server.Containers.ItemSlots
{
    public class ItemSlotChanged : EntityEventArgs
    {
        public ItemSlotsComponent SlotsComponent;
        public string Name;
        public ItemSlot Slot;

        public ItemSlotChanged(ItemSlotsComponent slotsComponent, string name, ItemSlot slot)
        {
            SlotsComponent = slotsComponent;
            Name = name;
            Slot = slot;
        }
    }
}
