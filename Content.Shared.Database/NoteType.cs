namespace Content.Shared.Database;

/*
 * Editing the numbers here may obliterate DB records, you have been warned.
 * If you do have to edit the numbers for some reason, please create migrations.
 * Adding new types is fine (or even renaming), but do not remove or change them.
 */

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
    ///     Watchlist, a secret note that gets shown to online admins every time a player connects
    /// </summary>
    Watchlist = 1,

    /// <summary>
    ///     A message, type of note that gets explicitly shown to the player
    /// </summary>
    Message = 2,

    /// <summary>
    ///     A server ban, converted to a shared note
    /// </summary>
    ServerBan = 3,

    /// <summary>
    ///     A role ban, converted to a shared note
    /// </summary>
    RoleBan = 4,
}
