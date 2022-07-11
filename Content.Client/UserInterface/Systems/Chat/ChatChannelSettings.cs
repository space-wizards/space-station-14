using Content.Shared.Chat;

namespace Content.Client.UserInterface.Systems.Chat;

public static class ChatChannelSettings
{
    public static readonly Dictionary<ChatChannel, ChatChannelConfigData> Config = new()
    {
        {ChatChannel.Local, new (true)},
        {ChatChannel.Whisper, new (true)},
        {ChatChannel.Radio, new (true)},
        {ChatChannel.LOOC, new (true)},
        {ChatChannel.OOC, new (true)},
        {ChatChannel.Emotes, new (false)},
        {ChatChannel.Dead, new (false)},
        {ChatChannel.Admin, new (true)},
        {ChatChannel.Visual, new (false, true, false)},
        {ChatChannel.Damage, new (false, true, false)},
        {ChatChannel.Server, new (false, false, false)},
        {ChatChannel.Unspecified, new ( false, false, false)}
    };

    public struct ChatChannelConfigData
    {
        public bool ShowUnread;
        public bool CanBeFiltered;
        public bool CanBeSelected;
        public ChatChannelConfigData(bool showUnread, bool canBeFiltered = true, bool canBeSelected = true)
        {
            ShowUnread = showUnread;
            CanBeFiltered = canBeFiltered;
            CanBeSelected = canBeSelected;
        }
    }
}
