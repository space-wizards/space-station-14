using System.Threading.Channels;
using Content.Shared.Chat;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed class ChatUIController : UIController
{

    public static readonly ChannelSelectorData[] ChannelSelectorSetup = new[]
    {
        new ChannelSelectorData(ChatSelectChannel.Local, ChatChannel.Local, '.'),
        new ChannelSelectorData(ChatSelectChannel.Whisper, ChatChannel.Whisper, ','),
        new ChannelSelectorData(ChatSelectChannel.Console, ChatChannel.None, '/', true),
        new ChannelSelectorData(ChatSelectChannel.Emotes, ChatChannel.Emotes, '@'),
        new ChannelSelectorData(ChatSelectChannel.Radio, ChatChannel.Radio, ';', true),
        new ChannelSelectorData(ChatSelectChannel.OOC, ChatChannel.OOC, '['),
        new ChannelSelectorData(ChatSelectChannel.LOOC, ChatChannel.LOOC, '('),
        new ChannelSelectorData(ChatSelectChannel.Admin, ChatChannel.Admin, ']', true),
        new ChannelSelectorData(ChatSelectChannel.Dead, ChatChannel.Dead, '\\', true)
    };

    public static readonly ChannelFilterData[] ChannelFilterSetup = new[]
    {
        new ChannelFilterData(ChatChannel.Local, true),
        new ChannelFilterData(ChatChannel.Whisper,true),
        new ChannelFilterData(ChatChannel.Emotes,true),
        new ChannelFilterData(ChatChannel.Radio, true, true),
        new ChannelFilterData(ChatChannel.OOC, true),
        new ChannelFilterData(ChatChannel.LOOC, true),
        new ChannelFilterData(ChatChannel.Dead, true, true),
        new ChannelFilterData(ChatChannel.Damage, false, false, false),
        new ChannelFilterData(ChatChannel.Visual, false, false, false),
        new ChannelFilterData(ChatChannel.Admin, true, true),
        new ChannelFilterData(ChatChannel.Server)
    };

    public static readonly Dictionary<ChatChannel, ChatSelectChannel> ChannelToSelector;
    public static readonly Dictionary<ChatSelectChannel, ChatChannel> SelectorToChannel;
    public static readonly Dictionary<char, ChatSelectChannel> PrefixToSelector;
    public static readonly Dictionary<ChatSelectChannel, char> SelectorToPrefix;
    static ChatUIController()
    {
        ChannelToSelector = new();
        SelectorToChannel = new();
        PrefixToSelector = new();
        SelectorToPrefix = new();
        foreach (var channelSelectorData in ChannelSelectorSetup)
        {
            ChannelToSelector.Add(channelSelectorData.Channel, channelSelectorData.Selector);
            SelectorToChannel.Add(channelSelectorData.Selector, channelSelectorData.Channel);
            PrefixToSelector.Add(channelSelectorData.Prefix, channelSelectorData.Selector);
            SelectorToPrefix.Add(channelSelectorData.Selector,channelSelectorData.Prefix);
        }
    }

    public static string GetChannelSelectorName(ChatSelectChannel channelSelector)
    {
        return channelSelector.ToString();
    }

    public static char GetChannelSelectorPrefix(ChatSelectChannel channelSelector)
    {
        if (SelectorToPrefix.TryGetValue(channelSelector, out var prefix))
        {
            return prefix;
        }
        return default;
    }

    public struct ChannelFilterData
    {
        public ChatChannel Channel = ChatChannel.None;
        public bool InitialState = true;
        public bool Hidden = false;
        public bool ShowUnread = true;

        public ChannelFilterData(ChatChannel channel, bool initialState = true, bool hidden = false, bool showUnread = true)
        {
            Channel = channel;
            InitialState = initialState;
            Hidden = hidden;
            ShowUnread = showUnread;
        }
    }


    public struct ChannelSelectorData
    {
        public ChatSelectChannel Selector;
        public ChatChannel Channel;
        public char Prefix;
        public bool Hidden;

        public ChannelSelectorData(ChatSelectChannel selector, ChatChannel channel, char prefix, bool hidden = false)
        {
            Selector = selector;
            Channel = channel;
            Prefix = prefix;
            Hidden = hidden;
        }
    }

}
