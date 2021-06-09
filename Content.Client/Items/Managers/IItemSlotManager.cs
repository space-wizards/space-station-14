using Content.Client.Items.UI;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.Items.Managers
{
    public interface IItemSlotManager
    {
        bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity? item);
        void UpdateCooldown(ItemSlotButton? cooldownTexture, IEntity? entity);
        bool SetItemSlot(ItemSlotButton button, IEntity? entity);
        void HoverInSlot(ItemSlotButton button, IEntity? entity, bool fits);
    }
}
