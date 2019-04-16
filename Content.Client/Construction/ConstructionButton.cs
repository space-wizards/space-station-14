using Content.Client.GameObjects.Components.Construction;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Construction
{
    public class ConstructionButton : Button
    {
        private readonly IDisplayManager _displayManager;
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Construction/ConstructionButton.tscn");

        public ConstructorComponent Owner
        {
            get => Menu.Owner;
            set => Menu.Owner = value;
        }
        ConstructionMenu Menu;

        public ConstructionButton(IDisplayManager displayManager)
        {
            _displayManager = displayManager;
            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();

            OnPressed += IWasPressed;
        }

        private void PerformLayout()
        {
            Menu = new ConstructionMenu(_displayManager);
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
