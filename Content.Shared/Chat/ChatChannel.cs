using System;

namespace Content.Shared.Chat
{
    /// <summary>
    ///     Represents chat channels that the player can filter chat tabs by.
    /// </summary>
    [Flags]
    public enum ChatChannel : short
    {
        None = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local = 1,

        /// <summary>
        ///     Messages from the server
        /// </summary>
        Server = 2,

        /// <summary>
        ///     Damage messages
        /// </summary>
        Damage = 4,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = 8,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = 16,

        /// <summary>
        ///     Visual events the player can see.
        ///     Basically like visual_message in SS13.
        /// </summary>
        Visual = 32,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = 64,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = 128,

        /// <summary>
        ///     Admin chat
        /// </summary>
        AdminChat = 256,

        /// <summary>
        ///     Unspecified.
        /// </summary>
        Unspecified = 512,

        /// <summary>
        ///     Channels considered to be IC.
        /// </summary>
        IC = Local | Radio | Dead | Emotes | Damage | Visual,
    }
}
