using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;
using Robust.Server.Player;

namespace Content.Server.Administration.Notes;

public interface IAdminNotesManager
{
    event Action<SharedAdminNote>? NoteAdded;
    event Action<SharedAdminNote>? NoteModified;
    event Action<SharedAdminNote>? NoteDeleted;

    bool CanCreate(IPlayerSession admin);
    bool CanDelete(IPlayerSession admin);
    bool CanEdit(IPlayerSession admin);
    bool CanView(IPlayerSession admin);
    Task OpenEui(IPlayerSession admin, Guid notedPlayer);
    Task OpenUserNotesEui(IPlayerSession player);
    Task AddNote(IPlayerSession createdBy, Guid player, NoteType type, string message, NoteSeverity severity, bool secret, DateTime? expiryTime);
    Task DeleteNote(int noteId, IPlayerSession deletedBy);
    Task ModifyNote(int noteId, IPlayerSession editedBy, string message, NoteSeverity severity, bool secret, DateTime? expiryTime);
    /// <summary>
    /// Queries the database and retrieves all notes, secret and visible
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>ALL non-deleted notes, secret or not</returns>
    Task<List<AdminNote>> GetAllNotes(Guid player);
    /// <summary>
    /// Queries the database and retrieves the notes a player should see
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>All player-visible notes</returns>
    Task<List<AdminNote>> GetVisibleNotes(Guid player);
    /// <summary>
    /// Queries the database and retrieves watchlists that may have been placed on the player
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>Active watchlists</returns>
    Task<List<AdminNote>> GetActiveWatchlists(Guid player);
    /// <summary>
    /// Queries the database and retrieves new messages a player has gotten
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>All unread messages</returns>
    Task<List<AdminNote>> GetNewMessages(Guid player);
    Task<string> GetPlayerName(Guid player);
}
