using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Strip
{
    public sealed class StrippingMenu : DefaultWindow
    {
        public LayoutContainer InventoryContainer = new();
        public LayoutContainer HandsContainer = new();
        public BoxContainer ButtonContainer = new() { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(0, 0, 0, 5 ) };
        public bool Dirty = true;

        public event Action? OnDirty;

        public StrippingMenu()
        {
            var box = new BoxContainer() { Orientation = LayoutOrientation.Vertical };
            ContentsContainer.AddChild(box);
            box.AddChild(ButtonContainer);
            box.AddChild(HandsContainer);
            box.AddChild(InventoryContainer);
        }

        public void ClearButtons()
        {
            InventoryContainer.RemoveAllChildren();
            HandsContainer.RemoveAllChildren();
            ButtonContainer.RemoveAllChildren();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!Dirty)
                return;

            Dirty = false;
            OnDirty?.Invoke();
        }
    }
}
