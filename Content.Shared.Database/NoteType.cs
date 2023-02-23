namespace Content.Shared.Database
{
    /// <summary>
    ///     Different types of notes
    /// </summary>
    public enum NoteType
    {
        /// <summary>
        ///     Normal note
        /// </summary>
        Note = 0,

        /// <summary>
        ///     A message, type of note that gets explicitly shown to the player
        /// </summary>
        Message = 1,

        /// <summary>
        ///     Watchlist, a secret note that gets shown to online admins every time a player connects
        /// </summary>
        Watchlist = 2,
    }
}
