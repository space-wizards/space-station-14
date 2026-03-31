using System.Text.Json;
using Content.Shared.Database;

namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditEventWriteData
{
    public int ServerId { get; set; }
    public int? RoundId { get; set; }
    public Guid AdminUserId { get; set; }
    public AdminAuditAction Action { get; set; }
    public AuditSeverity Severity { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? TargetPlayerUserId { get; set; }
    public int? TargetEntityUid { get; set; }
    public string? TargetEntityName { get; set; }
    public string? TargetEntityPrototype { get; set; }
    public JsonDocument? Json { get; set; }
}
