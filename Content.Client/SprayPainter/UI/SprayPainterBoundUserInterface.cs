using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

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

        _window.OnSpritePicked += OnSpritePicked;
        _window.OnColorPicked += OnColorPicked;
        _window.OnTabChanged += OnTabChanged;
        _window.OnDecalChanged += OnDecalChanged;
        _window.OnDecalColorChanged += OnDecalColorChanged;
        _window.OnDecalAngleChanged += OnDecalAngleChanged;

        if (EntMan.TryGetComponent(Owner, out SprayPainterComponent? comp))
        {
            var sprayPainter = EntMan.System<SprayPainterSystem>();
            _window.Populate(sprayPainter.Entries, comp.Indexes, sprayPainter.Decals, comp.PickedColor, comp.ColorPalette, comp.SelectedTab);
        }
    }

    private void OnDecalAngleChanged(int angle)
    {
        SendMessage(new SprayPainterDecalAnglePickedMessage(angle));
    }

    private void OnDecalColorChanged(Color? color)
    {
        SendMessage(new SprayPainterDecalColorPickedMessage(color));
    }

    private void OnDecalChanged(ProtoId<DecalPrototype> protoId)
    {
        SendMessage(new SprayPainterDecalPickedMessage(protoId));
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
