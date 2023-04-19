using Content.Shared.Database;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed record SharedAdminNote(int Id, Guid Player, int? Round, string? ServerName, TimeSpan PlaytimeAtNote, NoteType NoteType, string Message, NoteSeverity NoteSeverity, bool Secret, string CreatedByName, string EditedByName, DateTime CreatedAt, DateTime LastEditedAt, DateTime? ExpiryTime);
