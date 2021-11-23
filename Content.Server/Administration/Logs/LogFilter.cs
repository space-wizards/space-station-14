using System;
using System.Collections.Generic;
using System.Threading;
using Content.Shared.Administration.Logs;

namespace Content.Server.Administration.Logs;

public sealed class LogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public string? Search { get; set; }

    public List<LogType>? Types { get; set; }

    public List<LogImpact>? Impacts { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? After { get; set; }

    public Guid[]? AnyPlayers { get; set; }

    public Guid[]? AllPlayers { get; set; }

    public int? LastLogId { get; set; }

    public int? Limit { get; set; }

    public DateOrder DateOrder { get; set; } = DateOrder.Descending;
}
