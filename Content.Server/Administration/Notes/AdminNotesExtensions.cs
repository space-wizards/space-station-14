using System.Diagnostics;
using Content.Server.Database;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;

namespace Content.Server.Administration.Notes;

public static class AdminNotesExtensions
{
    public static SharedAdminNote ToShared(this IAdminRemarksCommon note)
    {
        NoteSeverity? severity = null;
        var secret = false;
        NoteType type;
        string[]? bannedRoles = null;
        string? unbannedByName = null;
        DateTime? unbannedTime = null;
        bool? seen = null;
        switch (note)
        {
            case AdminNote adminNote:
                type = NoteType.Note;
                severity = adminNote.Severity;
                secret = adminNote.Secret;
                break;
            case AdminWatchlist:
                type = NoteType.Watchlist;
                secret = true;
                break;
            case AdminMessage adminMessage:
                type = NoteType.Message;
                seen = adminMessage.Seen;
                break;
            case ServerBanNote ban:
                type = NoteType.ServerBan;
                severity = ban.Severity;
                unbannedTime = ban.UnbanTime;
                unbannedByName = ban.UnbanningAdmin?.LastSeenUserName ?? Loc.GetString("system-user");
                break;
            case ServerRoleBanNote roleBan:
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
        if (note.PlayerUserId is null)
            throw new ArgumentNullException(nameof(note.PlayerUserId), "Player user ID cannot be null for a note");
        return new SharedAdminNote(
            note.Id,
            note.PlayerUserId.Value,
            note.RoundId,
            note.Round?.Server.Name,
            note.PlaytimeAtNote,
            type,
            note.Message,
            severity,
            secret,
            note.CreatedBy?.LastSeenUserName ?? Loc.GetString("system-user"),
            note.LastEditedBy?.LastSeenUserName ?? string.Empty,
            note.CreatedAt,
            note.LastEditedAt,
            note.ExpirationTime,
            bannedRoles,
            unbannedTime,
            unbannedByName,
            seen
        );
    }
}
