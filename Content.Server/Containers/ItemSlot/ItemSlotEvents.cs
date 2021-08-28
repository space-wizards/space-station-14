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

    /// <summary>
    ///     Try to eject item from slot to users hand.
    ///     If no users provided or user can't take item, it will drop item.
    /// </summary>
    public class EjectItemAttempt : CancellableEntityEventArgs
    {
        public string SlotName;
        public IEntity? User;

        public EjectItemAttempt(string slotName, IEntity? user = null)
        {
            SlotName = slotName;
            User = user;
        }
    }
}
