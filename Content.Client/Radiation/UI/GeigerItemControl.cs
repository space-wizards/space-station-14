using Content.Client.Message;
using Content.Client.Radiation.Components;
using Content.Client.Stylesheets;
using Content.Shared.Radiation.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Radiation.UI;

public sealed class GeigerItemControl : Control
{
    private readonly GeigerComponent _component;
    private readonly RichTextLabel _label;

    public GeigerItemControl(GeigerComponent component)
    {
        _component = component;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);

        Update();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_component.UiUpdateNeeded)
            return;
        Update();
    }

    private void Update()
    {
        var color = SharedGeigerSystem.RadsToColor(_component.CurrentRadiation);
        var rads = _component.CurrentRadiation.ToString("N1");
        var msg = Loc.GetString("geiger-item-control-status",
            ("rads", rads), ("color", color));

        _label.SetMarkup(msg);
        _component.UiUpdateNeeded = false;
    }
}
