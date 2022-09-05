using Content.Client.Chemistry.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class HyposprayStatusControl : Control
{
    private readonly HyposprayComponent _parent;
    private readonly RichTextLabel _label;

    public HyposprayStatusControl(HyposprayComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
        AddChild(_label);

        Update();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!_parent.UiUpdateNeeded)
            return;
        Update();
    }

    public void Update()
    {

        _parent.UiUpdateNeeded = false;

        _label.SetMarkup(Loc.GetString(
            "hypospray-volume-text",
            ("currentVolume", _parent.CurrentVolume),
            ("totalVolume", _parent.TotalVolume)));
    }
}
