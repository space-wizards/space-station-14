using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

[Serializable]
public record struct SharedAdminAuditLog(
    int Id,
    AdminAuditAction Action,
    AuditSeverity Severity,
    DateTime OccurredAt,
    Guid AdminUserId,
    string Message,
    Guid? TargetPlayerUserId,
    int? TargetEntityUid,
    string? TargetEntityName,
    string? TargetEntityPrototype);
