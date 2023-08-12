using Content.Shared.OmniPainter;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.OmniPainter.UI
{
    public sealed class OmniPainterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private OmniPainterWindow? _window;

        [ViewVariables]
        private OmniPainterSystem? _painter;

        public OmniPainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new OmniPainterWindow();

            _painter = EntMan.System<OmniPainterSystem>();

            _window.OpenCentered();
            _window.OnClose += Close;
            _window.OnSpritePicked = OnSpritePicked;
            _window.OnColorPicked = OnColorPicked;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null)
                return;

            if (_painter == null)
                return;

            if (state is not OmniPainterBoundUserInterfaceState stateCast)
                return;

            _window.Populate(_painter.Entries,
                             stateCast.SelectedStyle,
                             stateCast.SelectedColorKey,
                             stateCast.Palette);
        }

        private void OnSpritePicked(ItemList.ItemListSelectedEventArgs args)
        {
            SendMessage(new OmniPainterSpritePickedMessage(args.ItemIndex));
        }

        private void OnColorPicked(ItemList.ItemListSelectedEventArgs args)
        {
            var key = _window?.IndexToColorKey(args.ItemIndex);
            SendMessage(new OmniPainterColorPickedMessage(key));
        }
    }
}
