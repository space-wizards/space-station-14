using Robust.Client.UserInterface;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.Interfaces
{
    public interface IItemSlotManager
    {
        public void Initialize();
        public bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity item);
    }
}
