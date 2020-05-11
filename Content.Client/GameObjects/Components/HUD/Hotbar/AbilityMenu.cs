using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    public class AbilityMenu : SS14Window
    {
        public VBoxContainer VBox;

        public AbilityMenu()
        {
            var scroll = new ScrollContainer();
            Contents.AddChild(scroll);

            VBox = new VBoxContainer();
            scroll.AddChild(VBox);
        }
    }
}
