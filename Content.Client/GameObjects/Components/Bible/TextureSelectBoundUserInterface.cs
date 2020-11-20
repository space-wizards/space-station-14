using Content.Shared.GameObjects.Components.TextureSelect;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Bible
{
    public class TextureSelectBoundUserInterface : BoundUserInterface
    {
        public TextureSelectBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private TextureSelectMenu _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new TextureSelectMenu(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu.Populate((TextureSelectBoundUserInterfaceState) state);
        }

        public void SelectStyle(string style)
        {
            SendMessage(new TextureSelectMessage(style));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
