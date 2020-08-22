using Content.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.UserInterface
{
    public interface IItemSlotManager
    {
        bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity item);
        void UpdateCooldown(ItemSlotButton cooldownTexture, IEntity entity);
        bool SetItemSlot(ItemSlotButton button, IEntity entity);
        void HoverInSlot(ItemSlotButton button, IEntity entity, bool fits);
    }
}
