using System;
using System.Collections.Generic;
using System.Threading;

namespace Content.Server.Administration.Logs;

public sealed class LogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public HashSet<Guid>? AllPlayers { get; set; }

    public HashSet<Guid>? AnyPlayers { get; set; }

    public DateOrder DateOrder { get; set; }
}

public enum DateOrder
{
    Ascending = 0,
    Descending
}
