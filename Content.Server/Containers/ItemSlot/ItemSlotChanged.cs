using Robust.Shared.GameObjects;

namespace Content.Server.Containers.ItemSlots
{
    /// <summary>
    ///     Item was placed in or removed from one of the slots in <see cref="ItemSlotsComponent"/> 
    /// </summary>
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
