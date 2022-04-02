using Content.Client.Inventory;

namespace Content.Client.UserInterface.Controls
{
    public sealed class ItemSlotButton : ItemSlotControl
    {
        public ItemSlotButton(){}

        public ItemSlotButton(ClientInventorySystem.SlotData slotData)
        {
            ButtonTexturePath = slotData.TextureName;
            Blocked = slotData.Blocked;
            Highlight = slotData.Highlighted;
            StorageTexturePath = "slots/back";
            SlotName = slotData.SlotName;
        }
    }
}
