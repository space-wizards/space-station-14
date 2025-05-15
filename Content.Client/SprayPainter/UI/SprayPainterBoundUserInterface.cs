using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.SprayPainter.UI;

/// <summary>
/// A BUI for a spray painter. Allows selecting pipe colours, decals, and paintable object types sorted by category.
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

        var sprayPainter = EntMan.System<SprayPainterSystem>();
        _window.PopulateCategories(sprayPainter.PaintableStylesByGroup, sprayPainter.PaintableGroupsByCategory, sprayPainter.Decals);
        Update();

        if (EntMan.TryGetComponent(Owner, out SprayPainterComponent? sprayPainterComp))
            _window.SetSelectedTab(sprayPainterComp.SelectedTab);
    }

    public override void Update()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out SprayPainterComponent? sprayPainter))
            return;

        _window.PopulateColors(sprayPainter.ColorPalette);
        if (sprayPainter.PickedColor != null)
            _window.SelectColor(sprayPainter.PickedColor);
        _window.SetSelectedStyles(sprayPainter.StylesByGroup);
        _window.SetSelectedDecal(sprayPainter.SelectedDecal);
        _window.SetDecalAngle(sprayPainter.SelectedDecalAngle);
        _window.SetDecalColor(sprayPainter.SelectedDecalColor);
        _window.SetDecalSnap(sprayPainter.SnapDecals);
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

    private void OnSpritePicked(string group, string style)
    {
        SendPredictedMessage(new SprayPainterSetPaintableStyleMessage(group, style));
    }

    private void OnSetPipeColor(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendPredictedMessage(new SprayPainterSetPipeColorMessage(key));
    }
}
