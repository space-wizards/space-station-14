using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SprayPainter.UI;

public sealed class SprayPainterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SprayPainterWindow? _window;

    public SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SprayPainterWindow>();

        _window.OnSpritePicked = OnSpritePicked;
        _window.OnColorPicked = OnColorPicked;
        _window.OnTabChanged = OnTabChanged;

        if (EntMan.TryGetComponent(Owner, out SprayPainterComponent? comp))
        {
            _window.Populate(EntMan.System<SprayPainterSystem>().Entries, comp.Indexes, comp.PickedColor, comp.ColorPalette, comp.SelectedTab);
        }
    }

    private void OnTabChanged(int index)
    {
        SendMessage(new SprayPainterTabChangedMessage(index));
    }

    private void OnSpritePicked(string category, int index)
    {
        SendMessage(new SprayPainterSpritePickedMessage(category, index));
    }

    private void OnColorPicked(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendMessage(new SprayPainterColorPickedMessage(key));
    }
}
