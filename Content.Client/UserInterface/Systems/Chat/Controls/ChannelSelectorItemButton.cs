using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorItemButton : Button
{
    public readonly ChatSelectChannel Channel;
    public bool IsHidden { get; set; }
    public ChannelSelectorItemButton(ChatUIController.ChannelSelectorData selectorData)
    {
        Channel = selectorData.Selector;
        AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);
        IsHidden = selectorData.Hidden;
        Text = ChatUIController.GetChannelSelectorName(selectorData.Selector);
        var prefix = ChatUIController.GetChannelSelectorPrefix(selectorData.Selector);
        if (prefix != default) Text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));
    }
}
