using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

[Serializable, NetSerializable]
public sealed class AdminAuditLogsEuiState : EuiStateBase
{
    public AdminAuditLogsEuiState(int roundId, int maxRoundId, int totalLogs, string currentServerName)
    {
        RoundId = roundId;
        MaxRoundId = maxRoundId;
        TotalLogs = totalLogs;
        CurrentServerName = currentServerName;
    }

    public bool IsLoading { get; set; }

    public int RoundId { get; }

    public int MaxRoundId { get; }

    public int TotalLogs { get; }

    public string CurrentServerName { get; }
}

public static class AdminAuditLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class AuditLogsRequest : EuiMessageBase
    {
        public int? RoundId { get; set; }
        public string? Search { get; set; }
        public HashSet<AdminAuditAction>? Actions { get; set; }
        public HashSet<AuditSeverity>? Severities { get; set; }
        public Guid? AdminUserId { get; set; }
        public Guid? TargetPlayerUserId { get; set; }
        public DateOrder DateOrder { get; set; }
        public LogSearchMode SearchMode { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class NewAuditLogs : EuiMessageBase
    {
        public List<SharedAdminAuditLog> Logs { get; set; } = new();
        public bool Replace { get; set; }
        public bool HasNext { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class NextAuditLogsRequest : EuiMessageBase
    {
    }
}
