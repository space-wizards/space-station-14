using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed record SharedAdminNote(int Id, int? Round, string Message, string CreatedByName, string EditedByName, DateTime CreatedAt, DateTime LastEditedAt);
