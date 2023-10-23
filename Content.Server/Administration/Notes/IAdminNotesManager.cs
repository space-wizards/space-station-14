using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;
using Robust.Shared.Players;

namespace Content.Server.Administration.Notes;

public interface IAdminNotesManager
{
    event Action<SharedAdminNote>? NoteAdded;
    event Action<SharedAdminNote>? NoteModified;
    event Action<SharedAdminNote>? NoteDeleted;

    bool CanCreate(ICommonSession admin);
    bool CanDelete(ICommonSession admin);
    bool CanEdit(ICommonSession admin);
    bool CanView(ICommonSession admin);
    Task OpenEui(ICommonSession admin, Guid notedPlayer);
    Task OpenUserNotesEui(ICommonSession player);
    Task AddAdminRemark(ICommonSession createdBy, Guid player, NoteType type, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime);
    Task DeleteAdminRemark(int noteId, NoteType type, ICommonSession deletedBy);
    Task ModifyAdminRemark(int noteId, NoteType type, ICommonSession editedBy, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime);
    /// <summary>
    /// Queries the database and retrieves all notes, secret and visible
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>ALL non-deleted notes, secret or not</returns>
    Task<List<IAdminRemarksCommon>> GetAllAdminRemarks(Guid player);
    /// <summary>
    /// Queries the database and retrieves the notes a player should see
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>All player-visible notes</returns>
    Task<List<IAdminRemarksCommon>> GetVisibleRemarks(Guid player);
    /// <summary>
    /// Queries the database and retrieves watchlists that may have been placed on the player
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>Active watchlists</returns>
    Task<List<AdminWatchlist>> GetActiveWatchlists(Guid player);
    /// <summary>
    /// Queries the database and retrieves new messages a player has gotten
    /// </summary>
    /// <param name="player">Desired player's <see cref="Guid"/></param>
    /// <returns>All unread messages</returns>
    Task<List<AdminMessage>> GetNewMessages(Guid player);
    Task MarkMessageAsSeen(int id);
}
