using System.Text.Json;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogEventWriteData
{
    public int LogId { get; set; }
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public int RoundId { get; set; }
    public LogType Type { get; set; }
    public LogImpact Impact { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public JsonDocument Json { get; set; } = default!;
    public List<Guid> Players { get; set; } = default!;
    public List<AdminLogEventEntityWriteData> Entities { get; set; } = default!;

    /// <summary>
    /// Tells the database what each player's role was in this event.
    /// Null when there's only one player or the roles aren't known.
    /// </summary>
    public Dictionary<Guid, AdminLogEntityRole>? PlayerRoles { get; set; }
}

public sealed class AdminLogEventEntityWriteData
{
    public required int EntityUid { get; init; }
    public required AdminLogEntityRole Role { get; init; }
    public string? PrototypeId { get; init; }
    public string? EntityName { get; init; }
}
