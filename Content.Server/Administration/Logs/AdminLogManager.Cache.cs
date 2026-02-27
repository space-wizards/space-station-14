using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Prometheus;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    private const int MaxRoundsCached = 3;
    private const int LogListInitialSize = 30_000;

    private readonly int _logTypes = Enum.GetValues<LogType>().Length;

    // TODO ADMIN LOGS make this thread safe or remove thread safety from the main partial class
    private readonly Dictionary<int, List<SharedAdminLog>> _roundsLogCache = new(MaxRoundsCached);
    private readonly Queue<int> _roundsLogCacheQueue = new();

    private static readonly Gauge CacheRoundCount = Metrics.CreateGauge(
        "admin_logs_cache_round_count",
        "How many rounds are in cache.");

    private static readonly Gauge CacheLogCount = Metrics.CreateGauge(
        "admin_logs_cache_log_count",
        "How many logs are in cache.");

    // TODO ADMIN LOGS cache previous {MaxRoundsCached} rounds on startup
    public void CacheNewRound()
    {
        List<SharedAdminLog>? list = null;

        _roundsLogCacheQueue.Enqueue(_currentRoundId);
        if (_roundsLogCacheQueue.Count > MaxRoundsCached)
        {
            var oldestRound = _roundsLogCacheQueue.Dequeue();
            if (_roundsLogCache.Remove(oldestRound, out var oldestList))
            {
                list = oldestList;
                list.Clear();
            }
        }

        list ??= new List<SharedAdminLog>(LogListInitialSize);

        _roundsLogCache.Add(_currentRoundId, list);
        CacheRoundCount.Set(_roundsLogCache.Count);
    }

    private void CacheLog(AdminLogEventWriteData log)
    {
        var players = log.Players.ToArray();
        var entities = log.Entities.Select(entity => new SharedAdminLogEntity(entity.EntityUid, entity.Role, entity.PrototypeId, entity.EntityName)).ToArray();

        var record = new SharedAdminLog(0, log.Type, log.Impact, log.OccurredAt, log.Message, players, entities);

        CacheLog(record);
    }

    private void CacheLog(SharedAdminLog log)
    {
        // TODO ADMIN LOGS remove redundant data and don't do a dictionary lookup per log
        var cache = _roundsLogCache[_currentRoundId];
        cache.Add(log);
        CacheLogCount.Set(cache.Count);
    }

    private void CacheLogs(IEnumerable<SharedAdminLog> logs)
    {
        var cache = _roundsLogCache[_currentRoundId];
        cache.AddRange(logs);
        CacheLogCount.Set(cache.Count);
    }

    private bool TryGetCache(int roundId, [NotNullWhen(true)] out List<SharedAdminLog>? cache)
    {
        return _roundsLogCache.TryGetValue(roundId, out cache);
    }

    private bool TrySearchCache(LogFilter? filter, [NotNullWhen(true)] out List<SharedAdminLog>? results)
    {
        //Logs V2 is keyset + participant first Old cache filtering is obsolete
        results = null;
        return false;
    }
}
