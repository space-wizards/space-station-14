using Content.Shared.EngineerPainter;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.EngineerPainter.UI
{
    public sealed class EngineerPainterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private EngineerPainterWindow? _window;

        [ViewVariables]
        private EngineerPainterSystem? _painter;

        public EngineerPainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new EngineerPainterWindow();

            _painter = EntMan.System<EngineerPainterSystem>();

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

            if (state is not EngineerPainterBoundUserInterfaceState stateCast)
                return;

            _window.Populate(_painter.Entries,
                             stateCast.SelectedStyle,
                             stateCast.SelectedColorKey,
                             stateCast.Palette);
        }

        private void OnSpritePicked(ItemList.ItemListSelectedEventArgs args)
        {
            SendMessage(new EngineerPainterSpritePickedMessage(args.ItemIndex));
        }

        private void OnColorPicked(ItemList.ItemListSelectedEventArgs args)
        {
            var key = _window?.IndexToColorKey(args.ItemIndex);
            SendMessage(new EngineerPainterColorPickedMessage(key));
        }
    }
}
