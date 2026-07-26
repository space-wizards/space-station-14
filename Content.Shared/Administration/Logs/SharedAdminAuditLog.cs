using Content.Shared.Database;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

[Serializable, NetSerializable]
public record struct SharedAdminAuditLog(
    int Id,
    AdminAuditAction Action,
    AuditSeverity Severity,
    DateTime OccurredAt,
    Guid AdminUserId,
    string AdminUserName,
    string Message,
    Guid? TargetPlayerUserId,
    string? TargetPlayerUserName,
    int? TargetEntityUid,
    string? TargetEntityName,
    string? TargetEntityPrototype,
    string? PayloadJson = null,
    string[]? PayloadLines = null);
