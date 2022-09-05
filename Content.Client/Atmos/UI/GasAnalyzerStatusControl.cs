using Content.Client.Atmos.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Atmos.UI;

public sealed class GasAnalyzerStatusControl : Control
{
    private readonly GasAnalyzerComponent _parent;
    private readonly RichTextLabel _label;

    public GasAnalyzerStatusControl(GasAnalyzerComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
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

        var color = _parent.Danger switch
        {
            SharedGasAnalyzerComponent.GasAnalyzerDanger.Warning => "orange",
            SharedGasAnalyzerComponent.GasAnalyzerDanger.Hazard => "red",
            _ => "green",
        };

        _label.SetMarkup(Loc.GetString("itemstatus-pressure-warn",
            ("color", color), ("danger", _parent.Danger)));
    }
}
