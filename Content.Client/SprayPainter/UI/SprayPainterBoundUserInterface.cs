using Content.Shared.SprayPainter;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SprayPainter.UI;

public sealed class SprayPainterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SprayPainterWindow? _window;

    [ViewVariables]
    private SprayPainterSystem? _painter;

    public SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new SprayPainterWindow();

        _painter = EntMan.System<SprayPainterSystem>();

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

        if (state is not SprayPainterBoundUserInterfaceState stateCast)
            return;

        _window.Populate(_painter.Entries,
                         stateCast.SelectedStyle,
                         stateCast.SelectedColorKey,
                         stateCast.Palette);
    }

    private void OnSpritePicked(ItemList.ItemListSelectedEventArgs args)
    {
        SendMessage(new SprayPainterSpritePickedMessage(args.ItemIndex));
    }

    private void OnColorPicked(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendMessage(new SprayPainterColorPickedMessage(key));
    }
}
