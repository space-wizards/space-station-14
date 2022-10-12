using Content.Client.Message;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.Systems;
using Content.Client.Stylesheets;
using Content.Shared.Radiation.Components;
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
        var danger = GeigerSystem.RadsToLevel(_component.CurrentRadiation);
        var color = DangerToColor(danger);

        var rads = _component.CurrentRadiation.ToString("N1");
        var msg = Loc.GetString("geiger-item-control-status",
            ("rads", rads), ("color", color));

        _label.SetMarkup(msg);
        _component.UiUpdateNeeded = false;
    }

    private Color DangerToColor(GeigerDangerLevel level)
    {
        switch (level)
        {
            case GeigerDangerLevel.None:
                return Color.Green;
            case GeigerDangerLevel.Low:
                return Color.Yellow;
            case GeigerDangerLevel.Med:
                return Color.DarkOrange;
            case GeigerDangerLevel.High:
            case GeigerDangerLevel.Extreme:
                return Color.Red;
            default:
                return Color.White;
        }
    }
}
