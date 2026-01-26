using System.Collections.Immutable;
using System.Linq;
using Content.Server.Database;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;

namespace Content.Server.Administration.Notes;

public static class AdminNotesExtensions
{
    public static SharedAdminNote ToShared(this IAdminRemarksRecord note)
    {
        NoteSeverity? severity = null;
        var secret = false;
        NoteType type;
        ImmutableArray<BanRoleDef>? bannedRoles = null;
        string? unbannedByName = null;
        DateTime? unbannedTime = null;
        bool? seen = null;
        switch (note)
        {
            case AdminNoteRecord adminNote:
                type = NoteType.Note;
                severity = adminNote.Severity;
                secret = adminNote.Secret;
                break;
            case AdminWatchlistRecord:
                type = NoteType.Watchlist;
                secret = true;
                break;
            case AdminMessageRecord adminMessage:
                type = NoteType.Message;
                seen = adminMessage.Seen;
                break;
            case BanNoteRecord { Type: BanType.Server } ban:
                type = NoteType.ServerBan;
                severity = ban.Severity;
                unbannedTime = ban.UnbanTime;
                unbannedByName = ban.UnbanningAdmin?.LastSeenUserName ?? Loc.GetString("system-user");
                break;
            case BanNoteRecord { Type: BanType.Role } roleBan:
                type = NoteType.RoleBan;
                severity = roleBan.Severity;
                bannedRoles = roleBan.Roles;
                unbannedTime = roleBan.UnbanTime;
                unbannedByName = roleBan.UnbanningAdmin?.LastSeenUserName ?? Loc.GetString("system-user");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), note.GetType(), "Unknown note type");
        }

        // There may be bans without a user, but why would we ever be converting them to shared notes?
        if (note.Players.Length == 0)
            throw new ArgumentNullException(nameof(note), "Player user ID cannot be empty for a note");

        return new SharedAdminNote(
            note.Id,
            [..note.Players.Select(p => p.UserId)],
            [..note.Rounds.Select(r => r.Id)],
            note.Rounds.SingleOrDefault()?.Server.Name, // TODO: Show all server names?
            note.PlaytimeAtNote,
            type,
            note.Message,
            severity,
            secret,
            note.CreatedBy?.LastSeenUserName ?? Loc.GetString("system-user"),
            note.LastEditedBy?.LastSeenUserName ?? string.Empty,
            note.CreatedAt.UtcDateTime,
            note.LastEditedAt?.UtcDateTime,
            note.ExpirationTime?.UtcDateTime,
            bannedRoles,
            unbannedTime,
            unbannedByName,
            seen
        );
    }
}
