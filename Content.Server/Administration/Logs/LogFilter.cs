using System.Threading;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public sealed class LogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public int? ServerId { get; set; }

    public string? Search { get; set; }

    public LogSearchMode SearchMode { get; set; } = LogSearchMode.Keyword;

    public HashSet<LogType>? Types { get; set; }

    public HashSet<LogImpact>? Impacts { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? After { get; set; }

    public bool IncludePlayers  { get; set; } = true;

    public int[]? AnyEntities { get; set; }

    public int[]? AllEntities { get; set; }

    public HashSet<AdminLogEntityRole>? EntityRoles { get; set; }

    public Guid[]? AnyPlayers { get; set; }

    public Guid[]? AllPlayers { get; set; }

    public bool IncludeNonPlayers { get; set; }

    /// <summary>
    /// Keyset cursor: the Id of the last log returned in the previous page.
    /// When <see cref="LastOccurredAt"/> is also set, the query uses a compound
    /// <c>(OccurredAt, Id)</c> cursor that can seek directly into the
    /// <c>(ServerId, OccurredAt, Id)</c> index.
    /// </summary>
    public int? LastLogId { get; set; }

    /// <summary>
    /// Keyset cursor: the OccurredAt timestamp of the last log returned.
    /// Must be set together with <see cref="LastLogId"/> for compound keyset pagination.
    /// </summary>
    public DateTime? LastOccurredAt { get; set; }

    public int LogsSent { get; set; }

    public int? Limit { get; set; }

    public DateOrder DateOrder { get; set; } = DateOrder.Descending;
}
