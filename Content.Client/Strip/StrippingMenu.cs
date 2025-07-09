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
        public BoxContainer SnareContainer = new();
        public bool Dirty = true;

        public event Action? OnDirty;

        public StrippingMenu()
        {
            var box = new BoxContainer() { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(0, 8) };
            Contents.AddChild(box);
            box.AddChild(SnareContainer);
            box.AddChild(HandsContainer);
            box.AddChild(InventoryContainer);
        }

        public void ClearButtons()
        {
            InventoryContainer.DisposeAllChildren();
            HandsContainer.DisposeAllChildren();
            SnareContainer.DisposeAllChildren();
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
