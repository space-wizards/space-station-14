using Content.Server.Database;
using Content.Shared.Administration.Notes;

namespace Content.Server.Administration.Notes;

public static class AdminNotesExtensions
{
    public static SharedAdminNote ToShared(this AdminNote note)
    {
        return new SharedAdminNote(
            note.Id,
            note.PlayerUserId,
            note.RoundId,
            note.Round?.Server.Name,
            note.PlaytimeAtNote,
            note.NoteType,
            note.Message,
            note.NoteSeverity,
            note.Secret,
            note.CreatedBy.LastSeenUserName,
            note.LastEditedBy.LastSeenUserName,
            note.CreatedAt,
            note.LastEditedAt,
            note.ExpiryTime
        );
    }
}
