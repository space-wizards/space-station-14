using Content.Client.GameObjects.Components.Construction;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Utility;

namespace Content.Client.Construction
{
    public class ConstructionButton : Button
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Construction/ConstructionButton.tscn");

        public ConstructorComponent Owner
        {
            get => Menu.Owner;
            set => Menu.Owner = value;
        }
        ConstructionMenu Menu;

        protected override void Initialize()
        {
            base.Initialize();

            OnPressed += IWasPressed;
            Menu = new ConstructionMenu();
            Menu.AddToScreen();
        }

        void IWasPressed(ButtonEventArgs args)
        {
            Menu.Open();
        }

        public void AddToScreen()
        {
            UserInterfaceManager.StateRoot.AddChild(this);
        }

        public void RemoveFromScreen()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Menu.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
