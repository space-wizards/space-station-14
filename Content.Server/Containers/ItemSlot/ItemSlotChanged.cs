using Robust.Shared.GameObjects;

namespace Content.Server.Containers.ItemSlot
{
    public class ItemSlotChanged : EntityEventArgs
    {
        public ItemSlotComponent Slot;
        public IEntity? Item;

        public ItemSlotChanged(ItemSlotComponent slot, IEntity? item)
        {
            Slot = slot;
            Item = item;
        }
    }
}
