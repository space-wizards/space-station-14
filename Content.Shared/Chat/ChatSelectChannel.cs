using System;

namespace Content.Shared.Chat
{
    /// <summary>
    ///     Chat channels that the player can select in the chat box.
    /// </summary>
    /// <remarks>
    ///     Maps to <see cref="ChatChannel"/>, giving better names.
    /// </remarks>
    [Flags]
    public enum ChatSelectChannel : ushort
    {
        None = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local = ChatChannel.Local,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = ChatChannel.Radio,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = ChatChannel.OOC,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = ChatChannel.Emotes,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = ChatChannel.Dead,

        /// <summary>
        ///     Admin chat
        /// </summary>
        Admin = ChatChannel.Admin,

        Console = ChatChannel.Unspecified
    }
}
