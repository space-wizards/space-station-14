using System.Threading;

namespace Content.Server.Administration.Logs;

public sealed class LogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public string? Type { get; set; }

    public DateOrder DateOrder { get; set; } = DateOrder.Ascending;
}

public enum DateOrder
{
    Ascending = 0,
    Descending
}
