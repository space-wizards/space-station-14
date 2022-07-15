using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Strip
{
    public sealed class StrippingMenu : DefaultWindow
    {
        public LayoutContainer InventoryContainer = new();
        public BoxContainer HandsContainer = new() { Orientation = LayoutOrientation.Horizontal };

        public StrippingMenu(string title)
        {
            Title = title;
            var box = new BoxContainer() { Orientation = LayoutOrientation.Vertical, Margin = new Thickness(0, 8) };
            Contents.AddChild(box);
            box.AddChild(HandsContainer);
            box.AddChild(InventoryContainer);
        }

        public void ClearButtons()
        {
            InventoryContainer.DisposeAllChildren();
            HandsContainer.DisposeAllChildren();
        }
    }
}
