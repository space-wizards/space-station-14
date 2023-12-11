using System.Numerics;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : Button
{
    private readonly IGameTiming _gameTiming;

    private readonly ChannelSelectorPopup _channelSelectorPopup;
    public event Action<ChatSelectChannel>? OnChannelSelect;

    public ChatSelectChannel SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    private uint _frameLastPopupChanged;

    public ChannelSelectorButton()
    {
        _gameTiming = IoCManager.Resolve<IGameTiming>();

        Name = "ChannelSelector";

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
        // If the popup gets closed (e.g. by clicking anywhere else on the screen)
        // We clear the button pressed state.

        Pressed = control.Visible;
        _frameLastPopupChanged = _gameTiming.CurFrame;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        // If you try to close the popup by clicking on the button again the following would happen:
        // The UI system would see that you clicked outside a popup, and would close it.
        // Because of the above logic, that sets the button to UNPRESSED.
        // THEN, it would propagate the keyboard event to us, the chat selector...
        // And we would become pressed again.
        // As a workaround, we check the frame the popup was last dismissed (above)
        // and don't allow changing it again this frame.
        if (_frameLastPopupChanged == _gameTiming.CurFrame)
            return;

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
        OnChannelSelect?.Invoke(channel);
    }

    public static string ChannelSelectorName(ChatSelectChannel channel)
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
            ChatSelectChannel.Admin => Color.HotPink,
            _ => Color.DarkGray
        };
    }

    public void UpdateChannelSelectButton(ChatSelectChannel channel, Shared.Radio.RadioChannelPrototype? radio)
    {
        Text = radio != null ? Loc.GetString(radio.Name) : ChannelSelectorName(channel);
        Modulate = radio?.Color ?? ChannelSelectColor(channel);
    }

    private void OnSelectorButtonToggled(ButtonToggledEventArgs args)
    {
        if (args.Pressed)
        {
            var globalLeft = GlobalPosition.X;
            var globalBot = GlobalPosition.Y + Height;
            var box = UIBox2.FromDimensions(new Vector2(globalLeft, globalBot), new Vector2(SizeBox.Width, SelectorDropdownOffset));
            _channelSelectorPopup.Open(box);
        }
        else
        {
            _channelSelectorPopup.Close();
        }
    }
}
