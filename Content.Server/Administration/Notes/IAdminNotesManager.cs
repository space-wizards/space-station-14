using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration.Notes;
using Robust.Server.Player;

namespace Content.Server.Administration.Notes;

public interface IAdminNotesManager
{
    event Action<SharedAdminNote>? NoteAdded;
    event Action<SharedAdminNote>? NoteModified;
    event Action<int>? NoteDeleted;

    bool CanCreate(IPlayerSession admin);
    bool CanDelete(IPlayerSession admin);
    bool CanEdit(IPlayerSession admin);
    bool CanView(IPlayerSession admin);
    Task OpenEui(IPlayerSession admin, Guid notedPlayer);
    Task AddNote(IPlayerSession createdBy, Guid player, string message);
    Task DeleteNote(int noteId, IPlayerSession deletedBy);
    Task ModifyNote(int noteId, IPlayerSession editedBy, string message);
    Task<List<AdminNote>> GetNotes(Guid player);
    Task<string> GetPlayerName(Guid player);
}
