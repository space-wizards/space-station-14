using Content.Shared.Chat;
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
    public event Action<ChatSelectChannel>? OnChannelSelect;

    public ChatSelectChannel SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    public ChannelSelectorButton()
    {
        // needed so the popup is untoggled regardless of which key is pressed when hovering this button.
        // If we don't have this, then right clicking the button while it's toggled on will hide
        // the popup but keep the button toggled on
        Name = "ChannelSelector";
        Mode = ActionMode.Press;
        EnableAllKeybinds = true;
        ToggleMode = true;
        OnToggled += OnSelectorButtonToggled;
        _channelSelectorPopup = UserInterfaceManager.CreatePopup<ChannelSelectorPopup>();
        _channelSelectorPopup.Selected += OnChannelSelected;
        _channelSelectorPopup.OnVisibilityChanged += OnPopupVisibilityChanged;

        if (_channelSelectorPopup.FirstChannel is { } firstSelector)
        {
            Select(firstSelector);
        }
    }

    private void OnChannelSelected(ChatSelectChannel channel)
    {
        Select(channel);
    }

    private void OnPopupVisibilityChanged(Control control)
    {
        Pressed = control.Visible;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
        if (args.Function == EngineKeyFunctions.Use) return;
        base.KeyBindDown(args);
    }

    public void Select(ChatSelectChannel channel)
    {
        if (_channelSelectorPopup.Visible)
        {
            _channelSelectorPopup.Close();
        }

        if (SelectedChannel == channel) return;
        SelectedChannel = channel;
        UpdateChannelSelectButton(channel);

        OnChannelSelect?.Invoke(channel);
    }

    public string ChannelSelectorName(ChatSelectChannel channel)
    {
        return Loc.GetString($"hud-chatbox-select-channel-{channel}");
    }

    public Color ChannelSelectColor(ChatSelectChannel channel)
    {
        return channel switch
        {
            ChatSelectChannel.Radio => Color.LimeGreen,
            ChatSelectChannel.LOOC => Color.MediumTurquoise,
            ChatSelectChannel.OOC => Color.LightSkyBlue,
            ChatSelectChannel.Dead => Color.MediumPurple,
            ChatSelectChannel.Admin => Color.Red,
            _ => Color.DarkGray
        };
    }

    public void UpdateChannelSelectButton(ChatSelectChannel channel)
    {
        Text = ChannelSelectorName(channel);
        Modulate = ChannelSelectColor(channel);
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
