using System;
using System.Collections.Generic;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public class AdminLogsEuiState : EuiStateBase
{
    public bool IsLoading { get; set; }

    public Dictionary<Guid, string> Players { get; set; } = new();
}

public static class AdminLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class NewLogs : EuiMessageBase
    {
        public NewLogs(SharedAdminLog[] logs, bool replace)
        {
            Logs = logs;
            Replace = replace;
        }

        public SharedAdminLog[] Logs { get; set; }
        public bool Replace { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class LogsRequest : EuiMessageBase
    {
        public LogsRequest(
            string? search,
            List<LogType>? types,
            DateTime? before,
            DateTime? after,
            Guid[]? anyPlayers,
            Guid[]? allPlayers,
            int? lastLogId,
            DateOrder dateOrder)
        {
            Search = search;
            Types = types;
            Before = before;
            After = after;
            AnyPlayers = anyPlayers is { Length: > 0 } ? anyPlayers : null;
            AllPlayers = allPlayers is { Length: > 0 } ? allPlayers : null;
            LastLogId = lastLogId;
            DateOrder = dateOrder;
        }

        public string? Search { get; set; }
        public List<LogType>? Types { get; set; }
        public DateTime? Before { get; set; }
        public DateTime? After { get; set; }
        public Guid[]? AnyPlayers { get; set; }
        public Guid[]? AllPlayers { get; set; }
        public int? LastLogId { get; set; }
        public DateOrder DateOrder { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class NextLogsRequest : EuiMessageBase
    {
    }
}
