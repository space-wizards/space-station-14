namespace Content.Shared.Chat
{
    /// <summary>
    ///     Chat channels that the player can select in the chat box.
    /// </summary>
    /// <remarks>
    ///     Maps to <see cref="ChatChannelFilter"/>, giving better names.
    /// </remarks>
    [Flags]
    public enum ChatSelectChannel : ushort
    {
        None = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local = ChatChannelFilter.Local,

        /// <summary>
        ///     Chat heard by players right next to each other
        /// </summary>
        Whisper = ChatChannelFilter.Whisper,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = ChatChannelFilter.Radio,

        /// <summary>
        ///     Local out-of-character channel
        /// </summary>
        LOOC = ChatChannelFilter.LOOC,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = ChatChannelFilter.OOC,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = ChatChannelFilter.Emotes,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = ChatChannelFilter.Dead,

        /// <summary>
        ///     Admin chat
        /// </summary>
        Admin = ChatChannelFilter.AdminChat,

        Console = ChatChannelFilter.Unspecified
    }
}
