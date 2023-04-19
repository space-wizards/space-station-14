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
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

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

    public async Task OpenUserNotesEui(IPlayerSession player)
    {
        var ui = new UserNotesEui();
        _euis.OpenEui(ui, player);

        await ui.UpdateNotes();
    }

    public async Task AddNote(IPlayerSession createdBy, Guid player, NoteType type, string message, NoteSeverity severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();
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
                severity = NoteSeverity.None;
                secret = false;
                break;
            case NoteType.Watchlist:
                severity = NoteSeverity.None;
                secret = true;
                break;
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
        var serverName = _config.GetCVar(CVars.GameHostName); // This could probably be done another way, but this is fine. For displaying only.
        var createdAt = DateTime.UtcNow;
        var playtime = (await _db.GetPlayTimes(player)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;
        var noteId = await _db.AddAdminNote(roundId, player, playtime, type, message, severity, secret, createdBy.UserId, createdAt, expiryTime);

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
            expiryTime
        );
        NoteAdded?.Invoke(note);
    }

    public async Task DeleteNote(int noteId, IPlayerSession deletedBy)
    {
        var note = await _db.GetAdminNote(noteId);
        if (note == null)
        {
            _sawmill.Info($"{deletedBy.Name} has tried to delete non-existent note {noteId}");
            return;
        }

        _sawmill.Info($"{deletedBy.Name} has deleted note {noteId}");

        var deletedAt = DateTime.UtcNow;
        await _db.DeleteAdminNote(noteId, deletedBy.UserId, deletedAt);

        var sharedNote = new SharedAdminNote(
            noteId,
            note.RoundId,
            note.PlayerUserId,
            note.Message,
            note.CreatedBy.LastSeenUserName,
            note.LastEditedBy.LastSeenUserName,
            note.CreatedAt,
            note.LastEditedAt
        );
        NoteDeleted?.Invoke(sharedNote);
    }

    public async Task ModifyNote(int noteId, IPlayerSession editedBy, string message, NoteSeverity severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();

        var note = await _db.GetAdminNote(noteId);

        // If the note doesn't exist or is the same, we skip updating it
        if (note == null ||
            note.Message == message &&
            note.NoteSeverity == severity &&
            note.Secret == secret &&
            note.ExpiryTime == expiryTime)
        {
            return;
        }

        var sb = new StringBuilder($"{editedBy.Name} has modified {note.NoteType} {noteId}");

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
        await _db.EditAdminNote(noteId, message, severity, secret, editedBy.UserId, editedAt, expiryTime);

        var sharedNote = new SharedAdminNote(
            noteId,
            note.PlayerUserId,
            note.RoundId,
            note.Round?.Server.Name,
            note.PlaytimeAtNote,
            note.NoteType,
            message,
            severity,
            secret,
            note.CreatedBy.LastSeenUserName,
            editedBy.Name,
            note.CreatedAt,
            note.LastEditedAt,
            expiryTime
        );
        NoteModified?.Invoke(sharedNote);
    }

    public async Task<List<AdminNote>> GetAllNotes(Guid player)
    {
        return await _db.GetAllAdminNotes(player);
    }

    public async Task<List<AdminNote>> GetVisibleNotes(Guid player)
    {
        var canSeeOwnNotes = _config.GetCVar(CCVars.SeeOwnNotes);
        if (!canSeeOwnNotes)
        {
            _sawmill.Warning($"Someone tried to call GetVisibleNotes for {player} when see_own_notes was false");
            return new List<AdminNote>();
        }
        return await _db.GetVisibleAdminNotes(player);
    }

    public async Task<List<AdminNote>> GetActiveWatchlists(Guid player)
    {
        return await _db.GetActiveWatchlists(player);
    }

    public async Task<List<AdminNote>> GetNewMessages(Guid player)
    {
        return await _db.GetMessages(player);
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
