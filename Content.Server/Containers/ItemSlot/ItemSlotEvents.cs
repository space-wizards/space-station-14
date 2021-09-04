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
        public readonly IEntity? ContainedItem;

        public ItemSlotChanged(ItemSlotsComponent slotsComponent, string slotName, ItemSlot slot)
        {
            SlotsComponent = slotsComponent;
            SlotName = slotName;
            Slot = slot;
            ContainedItem = slot.ContainerSlot.ContainedEntity;
        }
    }
}
