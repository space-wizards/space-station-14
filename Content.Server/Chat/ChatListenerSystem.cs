using Content.Shared.Chat;

namespace Content.Server.Chat
{
    public sealed class ChatMessageHeardNearbyEvent : EntityEventArgs
    {
        /// <summary>
        ///     message
        /// </summary>
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
