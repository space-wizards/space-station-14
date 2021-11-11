using System;
using System.Collections.Generic;
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
        var roundId = _ticker.RoundId;
        _db.AddAdminLog(log, roundId);
    }

    public override void Add<T>(T log, Guid playerId)
    {
        var roundId = _ticker.RoundId;
        _db.AddAdminLog(log, roundId, playerId);
    }

    public override void Add<T>(T log, List<Guid> players)
    {
        var roundId = _ticker.RoundId;
        _db.AddAdminLog(log, roundId, players);
    }

    public IEnumerable<LogRecord<T>> OfPlayer<T>(Guid id)
    {
        return _db.GetAdminLogsOfPlayer<T>(id).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord<T>> OfRound<T>(int id)
    {
        return _db.GetAdminLogsOfRound<T>(id).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord<T>> OfRoundAndPlayer<T>(int roundId, Guid playerId)
    {
        return _db.GetAdminLogsOfRoundAndPlayer<T>(roundId, playerId).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord<T>> OfRoundWithAllPlayers<T>(int roundId, params Guid[] playerIds)
    {
        return _db.GetAdminLogsOfRoundWithAllPlayers<T>(roundId, playerIds: playerIds).GetAwaiter().GetResult();
    }

    public IEnumerable<LogRecord<T>> WithAllPlayers<T>(params Guid[] ids)
    {
        return _db.GetAdminLogsOfPlayers<T>(playerIds: ids).GetAwaiter().GetResult();
    }
}
