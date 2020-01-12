using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.Interfaces
{
    public interface IItemSlotManager
    {
        public void Initialize();
        public bool OnButtonPressed(BaseButton.ButtonEventArgs args, IEntity item);
    }
}
