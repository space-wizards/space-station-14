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
        ///     OOC messages from the server
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
        ///     Notifications are non-diagetic messages are related to ingame content.
        ///     PDA pop-ups and anomaly infections are examples.
        /// </summary>
        Notifications = 1 << 8,

        /// <summary>
        ///     Diagetic auditory messages that are more general, such as station announcements.
        /// </summary>
        Announcements = 1 << 9,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = 1 << 10,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = 1 << 11,

        /// <summary>
        ///     Misc admin messages
        /// </summary>
        Admin = 1 << 12,

        /// <summary>
        ///     Admin alerts, messages likely of elevated importance to admins
        /// </summary>
        AdminAlert = 1 << 13,

        /// <summary>
        ///     Admin chat
        /// </summary>
        AdminChat = 1 << 14,

        /// <summary>
        ///     Unspecified.
        /// </summary>
        Unspecified = 1 << 15,

        // The compiled enums below can be utilized for detecting what kind of message is sent.

        /// <summary>
        ///     Channels considered to be auditory IC.
        /// </summary>
        AuditoryChat = Local | Whisper | Radio | Announcements,

        /// <summary>
        ///     Channels considered to be visual IC.
        /// </summary>
        VisualChat = Emotes | Visual,

        /// <summary>
        ///     Channels considered to be IC.
        /// </summary>
        IC = Local | Whisper | Radio | Dead | Emotes | Damage | Visual | Notifications | Announcements,

        /// <summary>
        ///     Channels related to admin work.
        /// </summary>
        AdminRelated = Admin | AdminAlert | AdminChat,
    }
}
