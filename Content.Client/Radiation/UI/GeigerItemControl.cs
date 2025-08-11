using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Radiation.Components;
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
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
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
        string msg;
        if (_component.IsEnabled)
        {
            var color = SharedGeigerSystem.LevelToColor(_component.DangerLevel);
            var currentRads = _component.CurrentRadiation;
            var rads = currentRads.ToString("N1");
            msg = Loc.GetString("geiger-item-control-status",
                ("rads", rads), ("color", color));
        }
        else
        {
            msg = Loc.GetString("geiger-item-control-disabled");
        }

        _label.SetMarkup(msg);
        _component.UiUpdateNeeded = false;
    }
}
