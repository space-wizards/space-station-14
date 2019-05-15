using Content.Client.GameObjects.Components.Construction;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Construction
{
    public class ConstructionButton : Button
    {
        private readonly IDisplayManager _displayManager;

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

            AnchorLeft = 1.0f;
            AnchorTop = 1.0f;
            AnchorRight = 1.0f;
            AnchorBottom = 1.0f;
            MarginLeft = -110.0f;
            MarginTop = -70.0f;
            MarginRight = -50.0f;
            MarginBottom = -50.0f;
            Text = "Crafting";
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
