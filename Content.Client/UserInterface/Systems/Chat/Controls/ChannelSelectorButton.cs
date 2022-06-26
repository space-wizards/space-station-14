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
    private readonly ChannelSelectorPopup _channelSelectorPopup;
    private IUserInterfaceManager _interfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
    private const int SelectorDropdownOffset = 38;
    public ChannelSelectorButton()
    {
        // needed so the popup is untoggled regardless of which key is pressed when hovering this button.
        // If we don't have this, then right clicking the button while it's toggled on will hide
        // the popup but keep the button toggled on
        Mode = ActionMode.Press;
        EnableAllKeybinds = true;
        ToggleMode = true;
        OnToggled += OnSelectorButtonToggled;
        _channelSelectorPopup = _interfaceManager.CreateNamedPopup<ChannelSelectorPopup>("ChannelSelectorPopup", (0, 0)) ??
                                throw new Exception("Tried to add channel selector popup while one already exists");
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
        if (args.Function == EngineKeyFunctions.Use)
            return;

        base.KeyBindDown(args);
    }

    private void OnSelectorButtonToggled(ButtonToggledEventArgs args)
    {
        if (args.Pressed)
        {
            var globalLeft = GlobalPosition.X;
            var globalBot = GlobalPosition.Y + Height;
            var box = UIBox2.FromDimensions((globalLeft, globalBot), (SizeBox.Width, SelectorDropdownOffset));
            _channelSelectorPopup.Open(box);
        }
        else
        {
            _channelSelectorPopup.Close();
        }

    }
}
