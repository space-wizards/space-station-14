using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Notes;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesManager : IAdminNotesManager, IPostInjectInit
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public const string SawmillId = "admin.notes";

    public event Action<SharedAdminNote>? NoteAdded;
    public event Action<SharedAdminNote>? NoteModified;
    public event Action<int>? NoteDeleted;

    private ISawmill _sawmill = default!;

    public bool CanCreate(IPlayerSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanDelete(IPlayerSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanEdit(IPlayerSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.EditNotes);
    }

    public bool CanView(IPlayerSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.ViewNotes);
    }

    public async Task OpenEui(IPlayerSession admin, Guid notedPlayer)
    {
        var ui = new AdminNotesEui();
        _euis.OpenEui(ui, admin);

        await ui.ChangeNotedPlayer(notedPlayer);
    }

    public async Task AddNote(IPlayerSession createdBy, Guid player, string message)
    {
        _sawmill.Info($"Player {createdBy.Name} added note with message {message}");

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? round = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var createdAt = DateTime.UtcNow;
        var noteId = await _db.AddAdminNote(round, player, message, createdBy.UserId, createdAt);

        var note = new SharedAdminNote(
            noteId,
            round,
            message,
            createdBy.Name,
            createdBy.Name,
            createdAt,
            createdAt
        );
        NoteAdded?.Invoke(note);
    }

    public async Task DeleteNote(int noteId, IPlayerSession deletedBy)
    {
        var note = await _db.GetAdminNote(noteId);
        if (note == null)
        {
            _sawmill.Info($"Player {deletedBy.Name} tried to delete non-existent note {noteId}");
            return;
        }

        _sawmill.Info($"Player {deletedBy.Name} deleted note {noteId}");

        var deletedAt = DateTime.UtcNow;
        await _db.DeleteAdminNote(noteId, deletedBy.UserId, deletedAt);

        NoteDeleted?.Invoke(noteId);
    }

    public async Task ModifyNote(int noteId, IPlayerSession editedBy, string message)
    {
        message = message.Trim();

        var note = await _db.GetAdminNote(noteId);
        if (note == null || note.Message == message)
        {
            return;
        }

        _sawmill.Info($"Player {editedBy.Name} modified note {noteId} with message {message}");

        var editedAt = DateTime.UtcNow;
        await _db.EditAdminNote(noteId, message, editedBy.UserId, editedAt);

        var sharedNote = new SharedAdminNote(
            noteId,
            note.RoundId,
            message,
            note.CreatedBy.LastSeenUserName,
            editedBy.Name,
            note.CreatedAt,
            note.LastEditedAt
        );
        NoteModified?.Invoke(sharedNote);
    }

    public async Task<List<AdminNote>> GetNotes(Guid player)
    {
        return await _db.GetAdminNotes(player);
    }

    public async Task<string> GetPlayerName(Guid player)
    {
        return (await _db.GetPlayerRecordByUserId(new NetUserId(player)))?.LastSeenUserName ?? string.Empty;
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
