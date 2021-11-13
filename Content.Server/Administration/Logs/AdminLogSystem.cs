using System.Collections.Generic;
using Content.Server.Database;
using Content.Server.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs;

public class AdminLogSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;

    [Dependency] private readonly GameTicker _ticker = default!;

    public void Add(LogType type, ref LogStringHandler handler)
    {
        // TODO ADMIN LOGGING batch all these adds per tick
        var roundId = _ticker.RoundId;
        var typeString = type.ToString();
        var data = handler.ToJson();
        var message = handler.ToStringAndClear();

        _db.AddAdminLog(roundId, typeString, message, data);
    }

    public IEnumerable<LogRecord> All<T>(LogFilter? filter = null)
    {
        return _db.GetAdminLogs<T>(filter).GetAwaiter().GetResult();
    }

    public IEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord> CurrentRound<T>(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _ticker.RoundId;
        return All<T>(filter);
    }

    public IEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _ticker.RoundId;
        return AllMessages(filter);
    }
}
