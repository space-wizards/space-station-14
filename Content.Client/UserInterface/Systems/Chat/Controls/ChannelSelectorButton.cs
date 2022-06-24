using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

/// <summary>
/// Only needed to avoid the issue where right click on the button closes the popup
/// but leaves the button highlighted.
/// </summary>
public sealed class ChannelSelectorButton : Button
{
    public ChannelSelectorButton()
    {
        // needed so the popup is untoggled regardless of which key is pressed when hovering this button.
        // If we don't have this, then right clicking the button while it's toggled on will hide
        // the popup but keep the button toggled on
        Mode = ActionMode.Press;
        EnableAllKeybinds = true;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
        if (args.Function == EngineKeyFunctions.Use)
            return;

        base.KeyBindDown(args);
    }
}
