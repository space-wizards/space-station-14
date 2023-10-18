using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Notes;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesManager : IAdminNotesManager, IPostInjectInit
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public const string SawmillId = "admin.notes";

    public event Action<SharedAdminNote>? NoteAdded;
    public event Action<SharedAdminNote>? NoteModified;
    public event Action<SharedAdminNote>? NoteDeleted;

    private ISawmill _sawmill = default!;

    public bool CanCreate(ICommonSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanDelete(ICommonSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanEdit(ICommonSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.EditNotes);
    }

    public bool CanView(ICommonSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.ViewNotes);
    }

    public async Task OpenEui(ICommonSession admin, Guid notedPlayer)
    {
        var ui = new AdminNotesEui();
        _euis.OpenEui(ui, admin);

        await ui.ChangeNotedPlayer(notedPlayer);
    }

    public async Task OpenUserNotesEui(ICommonSession player)
    {
        var ui = new UserNotesEui();
        _euis.OpenEui(ui, player);

        await ui.UpdateNotes();
    }

    public async Task AddAdminRemark(ICommonSession createdBy, Guid player, NoteType type, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();

        // There's a foreign key constraint in place here. If there's no player record, it will fail.
        // Not like there's much use in adding notes on accounts that have never connected.
        // You can still ban them just fine, which is why we should allow admins to view their bans with the notes panel
        if (await _db.GetPlayerRecordByUserId((NetUserId) player) is null)
            return;

        var sb = new StringBuilder($"{createdBy.Name} added a");

        if (secret && type == NoteType.Note)
        {
            sb.Append(" secret");
        }

        sb.Append($" {type} with message {message}");

        switch (type)
        {
            case NoteType.Note:
                sb.Append($" with {severity} severity");
                break;
            case NoteType.Message:
                severity = null;
                secret = false;
                break;
            case NoteType.Watchlist:
                severity = null;
                secret = true;
                break;
            case NoteType.ServerBan:
            case NoteType.RoleBan:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        if (expiryTime is not null)
        {
            sb.Append($" which expires on {expiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");
        }

        _sawmill.Info(sb.ToString());

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var serverName = _config.GetCVar(CCVars.AdminLogsServerName); // This could probably be done another way, but this is fine. For displaying only.
        var createdAt = DateTime.UtcNow;
        var playtime = (await _db.GetPlayTimes(player)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;
        int noteId;
        bool? seen = null;

        switch (type)
        {
            case NoteType.Note:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a note", nameof(severity));
                noteId = await _db.AddAdminNote(roundId, player, playtime, message, severity.Value, secret, createdBy.UserId, createdAt, expiryTime);
                break;
            case NoteType.Watchlist:
                secret = true;
                noteId = await _db.AddAdminWatchlist(roundId, player, playtime, message, createdBy.UserId, createdAt, expiryTime);
                break;
            case NoteType.Message:
                noteId = await _db.AddAdminMessage(roundId, player, playtime, message, createdBy.UserId, createdAt, expiryTime);
                seen = false;
                break;
            case NoteType.ServerBan: // Add bans using the ban panel, not note edit
            case NoteType.RoleBan:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        var note = new SharedAdminNote(
            noteId,
            player,
            roundId,
            serverName,
            playtime,
            type,
            message,
            severity,
            secret,
            createdBy.Name,
            createdBy.Name,
            createdAt,
            createdAt,
            expiryTime,
            null,
            null,
            null,
            seen
        );
        NoteAdded?.Invoke(note);
    }

    private async Task<SharedAdminNote?> GetAdminRemark(int id, NoteType type)
    {
        return type switch
        {
            NoteType.Note => (await _db.GetAdminNote(id))?.ToShared(),
            NoteType.Watchlist => (await _db.GetAdminWatchlist(id))?.ToShared(),
            NoteType.Message => (await _db.GetAdminMessage(id))?.ToShared(),
            NoteType.ServerBan => (await _db.GetServerBanAsNoteAsync(id))?.ToShared(),
            NoteType.RoleBan => (await _db.GetServerRoleBanAsNoteAsync(id))?.ToShared(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type")
        };
    }

    public async Task DeleteAdminRemark(int noteId, NoteType type, ICommonSession deletedBy)
    {
        var note = await GetAdminRemark(noteId, type);
        if (note == null)
        {
            _sawmill.Warning($"Player {deletedBy.Name} has tried to delete non-existent {type} {noteId}");
            return;
        }

        var deletedAt = DateTime.UtcNow;

        switch (type)
        {
            case NoteType.Note:
                await _db.DeleteAdminNote(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.Watchlist:
                await _db.DeleteAdminWatchlist(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.Message:
                await _db.DeleteAdminMessage(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.ServerBan:
                await _db.HideServerBanFromNotes(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.RoleBan:
                await _db.HideServerRoleBanFromNotes(noteId, deletedBy.UserId, deletedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        _sawmill.Info($"{deletedBy.Name} has deleted {type} {noteId}");
        NoteDeleted?.Invoke(note);
    }

    public async Task ModifyAdminRemark(int noteId, NoteType type, ICommonSession editedBy, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();

        var note = await GetAdminRemark(noteId, type);

        // If the note doesn't exist or is the same, we skip updating it
        if (note == null ||
            note.Message == message &&
            note.NoteSeverity == severity &&
            note.Secret == secret &&
            note.ExpiryTime == expiryTime)
        {
            return;
        }

        var sb = new StringBuilder($"{editedBy.Name} has modified {type} {noteId}");

        if (note.Message != message)
        {
            sb.Append($", modified message from {note.Message} to {message}");
        }

        if (note.Secret != secret)
        {
            sb.Append($", made it {(secret ? "secret" : "visible")}");
        }

        if (note.NoteSeverity != severity)
        {
            sb.Append($", updated the severity from {note.NoteSeverity} to {severity}");
        }

        if (note.ExpiryTime != expiryTime)
        {
            sb.Append(", updated the expiry time from ");
            if (note.ExpiryTime is null)
                sb.Append("never");
            else
                sb.Append($"{note.ExpiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");

            sb.Append(" to ");

            if (expiryTime is null)
                sb.Append("never");
            else
                sb.Append($"{expiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");
        }

        _sawmill.Info(sb.ToString());

        var editedAt = DateTime.UtcNow;

        switch (type)
        {
            case NoteType.Note:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a note", nameof(severity));
                await _db.EditAdminNote(noteId, message, severity.Value, secret, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.Watchlist:
                await _db.EditAdminWatchlist(noteId, message, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.Message:
                await _db.EditAdminMessage(noteId, message, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.ServerBan:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a ban", nameof(severity));
                await _db.EditServerBan(noteId, message, severity.Value, expiryTime, editedBy.UserId, editedAt);
                break;
            case NoteType.RoleBan:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a role ban", nameof(severity));
                await _db.EditServerRoleBan(noteId, message, severity.Value, expiryTime, editedBy.UserId, editedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        var newNote = note with
        {
            Message = message,
            NoteSeverity = severity,
            Secret = secret,
            LastEditedAt = editedAt,
            EditedByName = editedBy.Name,
            ExpiryTime = expiryTime
        };
        NoteModified?.Invoke(newNote);
    }

    public async Task<List<IAdminRemarksCommon>> GetAllAdminRemarks(Guid player)
    {
        return await _db.GetAllAdminRemarks(player);
    }

    public async Task<List<IAdminRemarksCommon>> GetVisibleRemarks(Guid player)
    {
        if (_config.GetCVar(CCVars.SeeOwnNotes))
        {
            return await _db.GetVisibleAdminNotes(player);
        }
        _sawmill.Warning($"Someone tried to call GetVisibleNotes for {player} when see_own_notes was false");
        return new List<IAdminRemarksCommon>();
    }

    public async Task<List<AdminWatchlist>> GetActiveWatchlists(Guid player)
    {
        return await _db.GetActiveWatchlists(player);
    }

    public async Task<List<AdminMessage>> GetNewMessages(Guid player)
    {
        return await _db.GetMessages(player);
    }

    public async Task MarkMessageAsSeen(int id)
    {
        await _db.MarkMessageAsSeen(id);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
