using Content.Client.Cooldown;
using Content.Client.HUD;
using Content.Client.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

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
        }
    }
}
