using System.Threading.Channels;
using Content.Shared.Chat;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed class ChatUIController : UIController
{
    public static readonly ChannelFilterData[] ChannelFilterAttributes =
    {
        new (ChatChannel.Local, true),
        new (ChatChannel.Whisper, true),
        new (ChatChannel.Emotes, true),
        new (ChatChannel.Radio, true),
        new (ChatChannel.OOC, true),
        new (ChatChannel.Dead, true, true, true),
        new (ChatChannel.Damage, false, false),
        new (ChatChannel.Visual, false, false),
        new (ChatChannel.Admin, true,true,true),
        new (ChatChannel.Server, true),
    };

    private static ChannelData[] channelData = new[]
    {
        new ChannelData(ChatChannel.Local, ChatSelectChannel.Local, '.', new ChannelFilterData(ChatChannel.Local, true)),
        new ChannelData(ChatChannel.Whisper, ChatSelectChannel.Whisper, ',', new ChannelFilterData(ChatChannel.Whisper, true)),
        new ChannelData(null, ChatSelectChannel.Console, '/', null),
        new ChannelData(ChatChannel.Emotes, ChatSelectChannel.Emotes, '@', new ChannelFilterData(ChatChannel.Emotes, true)),
        new ChannelData(ChatChannel.Radio, ChatSelectChannel.Radio, ';', new ChannelFilterData(ChatChannel.Radio, true)),
        new ChannelData(ChatChannel.OOC, ChatSelectChannel.OOC, '[', new ChannelFilterData(ChatChannel.OOC, true)),
        new ChannelData(ChatChannel.LOOC, ChatSelectChannel.LOOC, '(', new ChannelFilterData(ChatChannel.LOOC, true)),
        new ChannelData(ChatChannel.Dead, ChatSelectChannel.Dead, '\\', new ChannelFilterData(ChatChannel.Dead, true)),
        new ChannelData(ChatChannel.Damage, null, default, new ChannelFilterData(ChatChannel.Damage, false, false)),
        new ChannelData(ChatChannel.Visual, null, default, new ChannelFilterData(ChatChannel.Visual, false, false)),
        new ChannelData(ChatChannel.Admin, ChatSelectChannel.Admin, ']', new ChannelFilterData(ChatChannel.Admin, true,true,true)),
        new ChannelData(ChatChannel.Server, null, default, new ChannelFilterData(ChatChannel.Server, true))
    };
    public static readonly Dictionary<ChatChannel, ChatSelectChannel> ChannelToSelector;
    public static readonly Dictionary<ChatSelectChannel, ChatChannel> SelectorToChannel;
    public static readonly Dictionary<char, ChatSelectChannel> PrefixToSelector;
    public static readonly Dictionary<ChatSelectChannel, char> SelectorToPrefix;

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

    static ChatUIController()
    {
        ChannelToSelector = new();
        SelectorToChannel = new();
        PrefixToSelector = new();
        SelectorToPrefix = new();
        foreach (var data in channelData)
        {
            if (data.Channel != null)
            {
                if (data.Selector == null) continue;
                    ChannelToSelector.Add(data.Channel.Value,data.Selector.Value);
                    SelectorToChannel.Add(data.Selector.Value,data.Channel.Value);
                if (data.Prefix == '\0') continue;
                    SelectorToPrefix.Add(data.Selector.Value,data.Prefix);
                    PrefixToSelector.Add(data.Prefix, data.Selector.Value);
            }
            else
            {
                if (data.Prefix == '\0' || data.Selector == null) continue;
                    PrefixToSelector.Add(data.Prefix, data.Selector.Value);
                    SelectorToPrefix.Add(data.Selector.Value, data.Prefix);
            }
        }
    }

    public struct ChannelFilterData
    {
        public ChatChannel Channel = ChatChannel.None;
        public bool Enabled = true;
        public bool Hidden = false;
        public bool ShowUnread = true;
        public ChannelFilterData(ChatChannel channel, bool enabled, bool showUnread = true, bool hidden = false)
        {
            Channel = channel;
            Enabled = enabled;
            ShowUnread = showUnread;
            Hidden = hidden;
        }
    }



    private struct ChannelData
    {
        public ChatChannel? Channel;
        public ChatSelectChannel? Selector;
        public char Prefix;
        public ChannelFilterData? FilterData;

        public ChannelData(ChatChannel? channel, ChatSelectChannel? selector, char prefix, ChannelFilterData? filterData)
        {
            Channel = channel;
            Selector = selector;
            Prefix = prefix;
            FilterData = filterData;
        }


    }

}
