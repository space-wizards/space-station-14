using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelItemButton : Button
{
    public readonly ChatSelectChannel Channel;

    public ChannelItemButton(ChatSelectChannel channel)
    {
        Channel = channel;
        AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);
        //Text = ChatBox.ChannelSelectorName(channel);
        Text = ChatUIController.GetChannelSelectorName(channel);
        var prefix = ChatUIController.GetChannelSelectorPrefix(channel);

        //var prefix = ChatBox.GetPrefixFromChannel(channel);
        if (prefix != default) Text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));
    }
}
