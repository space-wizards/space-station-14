using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs;

public class AdminLogSystem : SharedAdminLogSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;

    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Add<T>(T log)
    {
        // TODO ADMIN LOGGING batch all these adds per tick?
        Add(log, new List<Guid>());
    }

    public override void Add<T>(T log, Guid playerId)
    {
        Add(log, new List<Guid> {playerId});
    }

    public override void Add<T>(T log, params Guid[] playerIds)
    {
        Add(log, playerIds.ToList());
    }

    public override void Add<T>(T log, List<Guid> players)
    {
        var roundId = _ticker.RoundId;
        _db.AddAdminLog(log, roundId, players);
    }

    public IEnumerable<LogRecord<T>> All<T>(LogFilter? filter = null)
    {
        return _db.GetAdminLogs<T>(filter).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord<T>> CurrentRound<T>(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _ticker.RoundId;
        return All<T>(filter);
    }

    public IEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter).GetAwaiter().GetResult();
    }

    public IEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _ticker.RoundId;
        return AllMessages(filter);
    }
}
