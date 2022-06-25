using Content.Shared.Chat;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed class ChatUIController : UIController
{
    public static readonly ChannelData[] ChannelAttributes =
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


    public struct ChannelData
    {
        public ChatChannel Channel = ChatChannel.None;
        public bool Enabled = true;
        public bool Hidden = false;
        public bool ShowUnread = true;
        public ChannelData(ChatChannel channel, bool enabled, bool showUnread = true, bool hidden = false)
        {
            Channel = channel;
            Enabled = enabled;
            ShowUnread = showUnread;
            Hidden = hidden;
        }
    }

}
