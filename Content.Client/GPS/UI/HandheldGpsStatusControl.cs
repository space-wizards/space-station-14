using Content.Client.GPS.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.GPS.UI;

public sealed class HandheldGpsStatusControl : Control
{
    private readonly Entity<HandheldGPSComponent> _parent;
    private readonly RichTextLabel _label;
    private float _updateDif;
    private readonly IEntityManager _entMan;

    public HandheldGpsStatusControl(Entity<HandheldGPSComponent> parent)
    {
        _parent = parent;
        _entMan = IoCManager.Resolve<IEntityManager>();
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
        UpdateGpsDetails();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateDif += args.DeltaSeconds;
        if (_updateDif < _parent.Comp.UpdateRate)
            return;

        _updateDif -= _parent.Comp.UpdateRate;

        UpdateGpsDetails();
    }

    private void UpdateGpsDetails()
    {
        var posText = "Error";
        if (_entMan.TryGetComponent(_parent, out TransformComponent? transComp))
        {
            var pos =  transComp.MapPosition;
            var x = (int) pos.X;
            var y = (int) pos.Y;
            posText = $"({x}, {y})";
        }
        _label.SetMarkup(Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText)));
    }
}
