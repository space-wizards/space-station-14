namespace Content.Shared.Chat
{
    /// <summary>
    ///     Represents chat channels that the player can filter chat tabs by.
    /// </summary>
    [Flags]
    public enum ChatChannel : ushort
    {
        None = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local = 1 << 0,

        /// <summary>
        ///     Chat heard by players right next to each other
        /// </summary>
        Whisper = 1 << 1,

        /// <summary>
        ///     Messages from the server
        /// </summary>
        Server = 1 << 2,

        /// <summary>
        ///     Damage messages
        /// </summary>
        Damage = 1 << 3,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = 1 << 4,

        /// <summary>
        ///     Local out-of-character channel
        /// </summary>
        LOOC = 1 << 5,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = 1 << 6,

        /// <summary>
        ///     Visual events the player can see.
        ///     Basically like visual_message in SS13.
        /// </summary>
        Visual = 1 << 7,

        /// <summary>
        ///     Notifications from things like the PDA.
        ///     Receiving a PDA message will send a notification to this channel for example
        /// </summary>
        Notifications = 1 << 8,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = 1 << 9,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = 1 << 10,

        /// <summary>
        ///     Misc admin messages
        /// </summary>
        Admin = 1 << 11,

        /// <summary>
        ///     Admin alerts, messages likely of elevated importance to admins
        /// </summary>
        AdminAlert = 1 << 12,

        /// <summary>
        ///     Admin chat
        /// </summary>
        AdminChat = 1 << 13,

        /// <summary>
        ///     Unspecified.
        /// </summary>
        Unspecified = 1 << 14,

        /// <summary>
        ///     Channels considered to be IC.
        /// </summary>
        IC = Local | Whisper | Radio | Dead | Emotes | Damage | Visual | Notifications,

        AdminRelated = Admin | AdminAlert | AdminChat,
    }

    /// <summary>
    /// Contains extension methods for <see cref="ChatChannel"/>
    /// </summary>
    public static class ChatChannelExt
    {
        /// <summary>
        /// Gets a string representation of a chat channel.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when this channel does not have a string representation set.</exception>
        public static string GetString(this ChatChannel channel)
        {
            return channel switch
            {
                ChatChannel.OOC => Loc.GetString("chat-channel-humanized-ooc"),
                ChatChannel.AdminChat => Loc.GetString("chat-channel-humanized-admin"),
                _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
            };
        }
    }
}
