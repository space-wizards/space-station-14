using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterCheckbox : CheckBox
{
    public readonly ChatChannel Channel;
    public bool ShowUnread { get; set; }
    public bool IsHidden { get; set; }
    public ChannelFilterCheckbox(ChatChannel channel, int? unreadCount, bool showUnread = true, bool isHidden = false)
    {
        Channel = channel;
        ShowUnread = showUnread;
        IsHidden = isHidden;
        Text = Loc.GetString($"hud-chatbox-channel-{Channel}");
        if (ShowUnread) UpdateText(unreadCount);
    }

    private void UpdateText(int? unread)
    {
        var name = Loc.GetString($"hud-chatbox-channel-{Channel}");

        if (unread > 0)
            // todo: proper fluent stuff here.
            name += " (" + (unread > 9 ? "9+" : unread) + ")";

        Text = name;
    }

    public void UpdateUnreadCount(int? unread)
    {
        if (ShowUnread) UpdateText(unread);
    }
}
