using Robust.Shared.GameObjects;

namespace Content.Server.Containers.ItemSlots
{
    /// <summary>
    ///     Item was placed in or removed from one of the slots in <see cref="ItemSlotsComponent"/> 
    /// </summary>
    public class ItemSlotChanged : EntityEventArgs
    {
        public ItemSlotsComponent SlotsComponent;
        public string SlotName;
        public ItemSlot Slot;

        public ItemSlotChanged(ItemSlotsComponent slotsComponent, string slotName, ItemSlot slot)
        {
            SlotsComponent = slotsComponent;
            SlotName = slotName;
            Slot = slot;
        }
    }

    /// <summary>
    ///     Try to place item inside slot in <see cref="ItemSlotsComponent"/> 
    /// </summary>
    public class PlaceItemAttempt : CancellableEntityEventArgs
    {
        public string SlotName;
        public IEntity Item;

        public PlaceItemAttempt(string slotName, IEntity item)
        {
            SlotName = slotName;
            Item = item;
        }
    }
}
