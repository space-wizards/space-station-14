using System.Threading;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.AuditLog;

public sealed class AuditLogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public int? ServerId { get; set; }

    public string? Search { get; set; }

    public LogSearchMode SearchMode { get; set; } = LogSearchMode.Keyword;

    public HashSet<AdminAuditAction>? Actions { get; set; }

    public HashSet<AuditSeverity>? Severities { get; set; }

    public Guid? AdminUserId { get; set; }

    public Guid? TargetPlayerUserId { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? After { get; set; }

    public int? LastLogId { get; set; }

    public DateTime? LastOccurredAt { get; set; }

    public int? Limit { get; set; }

    public DateOrder DateOrder { get; set; } = DateOrder.Descending;
}
