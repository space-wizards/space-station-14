using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.Tools.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Tools.UI;

public sealed class WelderStatusControl : Control
{
    private readonly WelderComponent _parent;
    private readonly RichTextLabel _label;

    public WelderStatusControl(WelderComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
        AddChild(_label);

        UpdateDraw();
    }

    /// <inheritdoc />
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_parent.UiUpdateNeeded)
        {
            return;
        }
        Update();
    }

    public void Update()
    {
        _parent.UiUpdateNeeded = false;

        var fuelCap = _parent.FuelCapacity;
        var fuel = _parent.Fuel;
        var lit = _parent.Lit;

        _label.SetMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
            ("colorName", fuel < fuelCap / 4f ? "darkorange" : "orange"),
            ("fuelLeft", Math.Round(fuel, 1)),
            ("fuelCapacity", fuelCap),
            ("status", Loc.GetString(lit ? "welder-component-on-examine-welder-lit-message" : "welder-component-on-examine-welder-not-lit-message"))));
    }
}
