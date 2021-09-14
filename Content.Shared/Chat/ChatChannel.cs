using System;

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
        ///     Messages from the server
        /// </summary>
        Server = 1 << 1,

        /// <summary>
        ///     Damage messages
        /// </summary>
        Damage = 1 << 2,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = 1 << 3,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = 1 << 4,

        /// <summary>
        ///     Visual events the player can see.
        ///     Basically like visual_message in SS13.
        /// </summary>
        Visual = 1 << 5,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = 1 << 6,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = 1 << 7,

        /// <summary>
        ///     Admin chat
        /// </summary>
        Admin = 1 << 8,

        /// <summary>
        ///     Unspecified.
        /// </summary>
        Unspecified = 1 << 9,

        /// <summary>
        ///     Channels considered to be IC.
        /// </summary>
        IC = Local | Radio | Dead | Emotes | Damage | Visual,
    }
}
