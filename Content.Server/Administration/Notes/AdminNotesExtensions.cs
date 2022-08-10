using Content.Server.Database;
using Content.Shared.Administration.Notes;

namespace Content.Server.Administration.Notes;

public static class AdminNotesExtensions
{
    public static SharedAdminNote ToShared(this AdminNote note)
    {
        return new SharedAdminNote(
            note.Id,
            note.RoundId,
            note.Message,
            note.CreatedBy.LastSeenUserName,
            note.LastEditedBy.LastSeenUserName,
            note.CreatedAt,
            note.LastEditedAt
        );
    }
}
