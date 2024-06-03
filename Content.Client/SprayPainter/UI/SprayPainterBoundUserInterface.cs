using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
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

        if (!EntMan.TryGetComponent<SprayPainterComponent>(Owner, out var comp))
            return;

        _window = new SprayPainterWindow();

        _painter = EntMan.System<SprayPainterSystem>();

        _window.OnClose += Close;
        _window.OnSpritePicked = OnSpritePicked;
        _window.OnColorPicked = OnColorPicked;

        _window.Populate(_painter.Entries, comp.Index, comp.PickedColor, comp.ColorPalette);

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Dispose();
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
