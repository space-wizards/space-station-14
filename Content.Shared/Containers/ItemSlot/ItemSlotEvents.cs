using Robust.Shared.GameObjects;

namespace Content.Shared.Containers.ItemSlots
{
    /// <summary>
    ///     Item was placed in or removed from one of the slots in <see cref="SharedItemSlotsComponent"/> 
    /// </summary>
    public class ItemSlotChanged : EntityEventArgs
    {
        public SharedItemSlotsComponent SlotsComponent;
        public string SlotName;
        public ItemSlot Slot;
        public readonly EntityUid? ContainedItem;

        public ItemSlotChanged(SharedItemSlotsComponent slotsComponent, string slotName, ItemSlot slot)
        {
            SlotsComponent = slotsComponent;
            SlotName = slotName;
            Slot = slot;
            ContainedItem = slot.ContainerSlot.ContainedEntity?.Uid;
        }
    }
}
