using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Remotes.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Remotes.UI;

public sealed class DoorRemoteStatusControl(Entity<DoorRemoteComponent> ent) : Control
{
    private RichTextLabel? _label;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_label == null)
        {
            _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
            AddChild(_label);
        }
        else if (!ent.Comp.IsStatusControlUpdateRequired)
            return;

        UpdateLabel(_label);

        ent.Comp.IsStatusControlUpdateRequired = false;
    }

    private void UpdateLabel(RichTextLabel label)
    {
        var modeStringLocalized = Loc.GetString(ent.Comp.Mode switch
        {
            OperatingMode.OpenClose => "door-remote-open-close-text",
            OperatingMode.ToggleBolts => "door-remote-toggle-bolt-text",
            OperatingMode.ToggleEmergencyAccess => "door-remote-emergency-access-text",
            OperatingMode.ToggleOvercharge => "door-remote-toggle-eletrify-text",
            _ => "door-remote-invalid-text"
        });

        label.SetMarkup(Loc.GetString("door-remote-mode-label", ("modeString", modeStringLocalized)));
    }
}
