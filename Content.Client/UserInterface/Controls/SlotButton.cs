using Content.Client.Inventory;

namespace Content.Client.UserInterface.Controls
{
    public sealed class SlotButton : SlotControl
    {
        public SlotButton(){}

        public SlotButton(ClientInventorySystem.SlotData slotData)
        {
            ButtonTexturePath = slotData.TextureName;
            Blocked = slotData.Blocked;
            Highlight = slotData.Highlighted;
            StorageTexturePath = "Slots/back";
            SlotName = slotData.SlotName;
        }
    }
}
