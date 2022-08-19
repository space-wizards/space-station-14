using Content.Shared.Chat;

namespace Content.Server.Chat
{
    /// <summary>
    /// Event raised on entities with the "ChatListener" component when they hear a nearby message
    /// </summary>
    [ByRefEvent]
    public struct ChatMessageHeardNearbyEvent
    {
        public ChatChannel Channel;

        public string Message;

        public string MessageWrap;

        public EntityUid Source;

        public bool HideChat;

        public ChatMessageHeardNearbyEvent(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat)
        {
            Channel = channel;
            Message = message;
            MessageWrap = messageWrap;
            Source = source;
            HideChat = hideChat;
        }
    }
}
