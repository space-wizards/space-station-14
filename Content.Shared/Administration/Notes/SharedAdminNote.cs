using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed record SharedAdminNote(int Id, int? Round, Guid Player, string Message, string CreatedByName, string EditedByName, DateTime CreatedAt, DateTime LastEditedAt);
