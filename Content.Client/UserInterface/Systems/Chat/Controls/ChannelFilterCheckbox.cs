using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterCheckbox : CheckBox
{
    public readonly ChatChannel Channel;
    public bool ShowUnread { get; set; }
    public bool IsHidden { get; set; }
    public ChannelFilterCheckbox(ChatUIController.ChannelFilterData filterData)
    {
        Channel = filterData.Channel;
        ShowUnread = filterData.ShowUnread;
        IsHidden = filterData.Hidden;
        Text = Loc.GetString($"hud-chatbox-channel-{Channel}");
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
