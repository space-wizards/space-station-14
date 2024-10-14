namespace Content.Shared.Database;

/// <summary>
/// Utility functions for the audit log that are useful to SS14.Admin
/// </summary>
public static class AuditLogUtil
{
    public static LogImpact LogImpactFromNoteSeverity(NoteSeverity severity)
    {
        return severity switch
        {
            NoteSeverity.High => LogImpact.High,
            NoteSeverity.Medium => LogImpact.Medium,
            NoteSeverity.Minor => LogImpact.Low,
            NoteSeverity.None => LogImpact.Low,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }

    public static LogImpact LogImpactForRemark(NoteType type, NoteSeverity? severity = null)
    {
        return type switch
        {
            NoteType.Note => severity != null ? LogImpactFromNoteSeverity(severity.Value) : LogImpact.Low,
            NoteType.Message => LogImpact.Low,
            NoteType.Watchlist => LogImpact.Extreme,
            NoteType.RoleBan => LogImpact.High,
            NoteType.ServerBan => LogImpact.Extreme,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static AuditLogType LogTypeForRemark(NoteType type)
    {
        return type switch
        {
            NoteType.Note => AuditLogType.Note,
            NoteType.Message => AuditLogType.Message,
            NoteType.Watchlist => AuditLogType.Watchlist,
            NoteType.RoleBan => AuditLogType.RoleBan,
            NoteType.ServerBan => AuditLogType.ServerBan,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
