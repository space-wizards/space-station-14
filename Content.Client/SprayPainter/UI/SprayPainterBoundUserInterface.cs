using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.SprayPainter.UI;

/// <summary>
/// A BUI for a spray painter. Allows selecting pipe colours, paintable object types by class, and decals.
/// </summary>
public sealed class SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SprayPainterWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            _window = this.CreateWindow<SprayPainterWindow>();

            _window.OnSpritePicked += OnSpritePicked;
            _window.OnSetPipeColor += OnSetPipeColor;
            _window.OnTabChanged += OnTabChanged;
            _window.OnDecalChanged += OnDecalChanged;
            _window.OnDecalColorChanged += OnDecalColorChanged;
            _window.OnDecalAngleChanged += OnDecalAngleChanged;
            _window.OnDecalSnapChanged += OnDecalSnapChanged;
        }

        if (EntMan.TryGetComponent(Owner, out SprayPainterComponent? comp))
        {
            var sprayPainter = EntMan.System<SprayPainterSystem>();
            _window.Populate(sprayPainter.Entries, comp.Indexes, sprayPainter.Decals, comp.PickedColor, comp.ColorPalette, comp.SelectedTab);
        }
    }

    private void OnDecalSnapChanged(bool snap)
    {
        SendPredictedMessage(new SprayPainterSetDecalSnapMessage(snap));
    }

    private void OnDecalAngleChanged(int angle)
    {
        SendPredictedMessage(new SprayPainterSetDecalAngleMessage(angle));
    }

    private void OnDecalColorChanged(Color? color)
    {
        SendPredictedMessage(new SprayPainterSetDecalColorMessage(color));
    }

    private void OnDecalChanged(ProtoId<DecalPrototype> protoId)
    {
        SendPredictedMessage(new SprayPainterSetDecalMessage(protoId));
    }

    private void OnTabChanged(int index, bool isSelectedTabWithDecals)
    {
        SendPredictedMessage(new SprayPainterTabChangedMessage(index, isSelectedTabWithDecals));
    }

    private void OnSpritePicked(string category, int index)
    {
        SendPredictedMessage(new SprayPainterSetPaintablePrototypeMessage(category, index));
    }

    private void OnSetPipeColor(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendPredictedMessage(new SprayPainterSetPipeColorMessage(key));
    }
}
