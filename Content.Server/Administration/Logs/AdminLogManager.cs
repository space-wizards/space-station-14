using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Prometheus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager : SharedAdminLogManager, IAdminLogManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDependencyCollection _dependencies = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;

    public const string SawmillId = "admin.logs";

    private static readonly Histogram DatabaseUpdateTime = Metrics.CreateHistogram(
        "admin_logs_database_time",
        "Time used to send logs to the database in ms",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.5, 20)
        });

    private static readonly Gauge Queue = Metrics.CreateGauge(
        "admin_logs_queue",
        "How many logs are in the queue.");

    private static readonly Gauge PreRoundQueue = Metrics.CreateGauge(
        "admin_logs_pre_round_queue",
        "How many logs are in the pre-round queue.");

    private static readonly Gauge QueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the log queue cap has been reached in a round.");

    private static readonly Gauge PreRoundQueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the pre-round log queue cap has been reached in a round.");

    private static readonly Gauge LogsSent = Metrics.CreateGauge(
        "admin_logs_sent",
        "Amount of logs sent to the database in a round.");

    // Init only
    private ISawmill _sawmill = default!;

    // CVars
    private bool _metricsEnabled;
    private bool _enabled;
    private TimeSpan _queueSendDelay;
    private int _queueMax;
    private int _preRoundQueueMax;
    private int _dropThreshold;
    private int _highImpactLogPlaytime;

    // Per update
    private TimeSpan _nextUpdateTime;
    private readonly ConcurrentQueue<AdminLog> _logQueue = new();
    private readonly ConcurrentQueue<AdminLog> _preRoundLogQueue = new();

    // Per round
    private int _currentRoundId;
    private int _currentLogId;
    private int NextLogId => Interlocked.Increment(ref _currentLogId);
    private GameRunLevel _runLevel = GameRunLevel.PreRoundLobby;

    // 1 when saving, 0 otherwise
    private int _savingLogs;
    private int _logsDropped;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        InitializeJson();

        _configuration.OnValueChanged(CVars.MetricsEnabled,
            value => _metricsEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsEnabled,
            value => _enabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueMax,
            value => _queueMax = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsPreRoundQueueMax,
            value => _preRoundQueueMax = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsDropThreshold,
            value => _dropThreshold = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsHighLogPlaytime,
            value => _highImpactLogPlaytime = value, true);

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Set(0);
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    public async Task Shutdown()
    {
        if (!_logQueue.IsEmpty)
        {
            await SaveLogs();
        }
    }

    public async void Update()
    {
        if (_runLevel == GameRunLevel.PreRoundLobby)
        {
            await PreRoundUpdate();
            return;
        }

        var count = _logQueue.Count;
        Queue.Set(count);

        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (count + preRoundCount == 0)
        {
            return;
        }

        if (_timing.RealTime >= _nextUpdateTime)
        {
            await TrySaveLogs();
            return;
        }

        if (count >= _queueMax)
        {
            if (_metricsEnabled)
            {
                QueueCapReached.Inc();
            }

            await TrySaveLogs();
        }
    }

    private async Task PreRoundUpdate()
    {
        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (preRoundCount < _preRoundQueueMax)
        {
            return;
        }

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Inc();
        }

        await TrySaveLogs();
    }

    private async Task TrySaveLogs()
    {
        if (Interlocked.Exchange(ref _savingLogs, 1) == 1)
            return;

        try
        {
            await SaveLogs();
        }
        finally
        {
            Interlocked.Exchange(ref _savingLogs, 0);
        }
    }

    private async Task SaveLogs()
    {
        _nextUpdateTime = _timing.RealTime.Add(_queueSendDelay);

        // TODO ADMIN LOGS array pool
        var copy = new List<AdminLog>(_logQueue.Count + _preRoundLogQueue.Count);
        copy.AddRange(_logQueue);

        if (_logQueue.Count >= _queueMax)
        {
            _sawmill.Warning($"In-round cap of {_queueMax} reached for admin logs.");
        }

        var dropped = Interlocked.Exchange(ref _logsDropped, 0);
        if (dropped > 0)
        {
            _sawmill.Error($"Dropped {dropped} logs. Current max threshold: {_dropThreshold}");
        }

        if (_runLevel == GameRunLevel.PreRoundLobby && !_preRoundLogQueue.IsEmpty)
        {
            _sawmill.Error($"Dropping {_preRoundLogQueue.Count} pre-round logs. Current cap: {_preRoundQueueMax}");
        }
        else
        {
            foreach (var log in _preRoundLogQueue)
            {
                log.RoundId = _currentRoundId;
                CacheLog(log);
            }

            copy.AddRange(_preRoundLogQueue);
        }

        _logQueue.Clear();
        Queue.Set(0);

        _preRoundLogQueue.Clear();
        PreRoundQueue.Set(0);

        var task = _db.AddAdminLogs(copy);

        _sawmill.Debug($"Saving {copy.Count} admin logs.");

        if (_metricsEnabled)
        {
            LogsSent.Inc(copy.Count);

            using (DatabaseUpdateTime.NewTimer())
            {
                await task;
                return;
            }
        }

        await task;
    }

    public void RoundStarting(int id)
    {
        _currentRoundId = id;
        CacheNewRound();
    }

    public void RunLevelChanged(GameRunLevel level)
    {
        _runLevel = level;

        if (level == GameRunLevel.PreRoundLobby)
        {
            Interlocked.Exchange(ref _currentLogId, 0);

            if (!_preRoundLogQueue.IsEmpty)
            {
                // This technically means that you could get pre-round logs from
                // a previous round passed onto the next one
                // If this happens please file a complaint with your nearest lottery
                foreach (var log in _preRoundLogQueue)
                {
                    log.Id = NextLogId;
                }
            }

            if (_metricsEnabled)
            {
                PreRoundQueueCapReached.Set(0);
                QueueCapReached.Set(0);
                LogsSent.Set(0);
            }
        }
    }

    private void Add(LogType type, LogImpact impact, string message, JsonDocument json, HashSet<Guid> players)
    {
        var preRound = _runLevel == GameRunLevel.PreRoundLobby;
        var count = preRound ? _preRoundLogQueue.Count : _logQueue.Count;
        if (count >= _dropThreshold)
        {
            Interlocked.Increment(ref _logsDropped);
            return;
        }

        var log = new AdminLog
        {
            Id = NextLogId,
            RoundId = _currentRoundId,
            Type = type,
            Impact = impact,
            Date = DateTime.UtcNow,
            Message = message,
            Json = json,
            Players = new List<AdminLogPlayer>(players.Count)
        };

        var adminLog = false;
        var adminSys = _entityManager.SystemOrNull<AdminSystem>();
        var logMessage = message;

        foreach (var id in players)
        {
            var player = new AdminLogPlayer
            {
                LogId = log.Id,
                PlayerUserId = id
            };

            log.Players.Add(player);

            if (adminSys != null)
            {
                var cachedInfo = adminSys.GetCachedPlayerInfo(new NetUserId(id));
                if (cachedInfo != null && cachedInfo.Antag)
                {
                    logMessage += " [ANTAG: " + cachedInfo.CharacterName + "]";
                }
            }

            if (adminLog)
                continue;

            if (impact == LogImpact.Extreme) // Always chat-notify Extreme logs
                adminLog = true;

            if (impact == LogImpact.High) // Only chat-notify High logs if the player is below a threshold playtime
            {
                if (_highImpactLogPlaytime >= 0 && _player.TryGetSessionById(new NetUserId(id), out var session))
                {
                    var playtimes = _playtime.GetPlayTimes(session);
                    if (playtimes.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var overallTime) &&
                        overallTime <= TimeSpan.FromHours(_highImpactLogPlaytime))
                    {
                        adminLog = true;
                    }
                }
            }
        }

        if (adminLog)
            _chat.SendAdminAlert(logMessage);

        if (preRound)
        {
            _preRoundLogQueue.Enqueue(log);
        }
        else
        {
            _logQueue.Enqueue(log);
            CacheLog(log);
        }
    }

    public override void Add(LogType type, LogImpact impact, ref LogStringHandler handler)
    {
        if (!_enabled)
        {
            handler.ToStringAndClear();
            return;
        }

        var (json, players) = ToJson(handler.Values);
        var message = handler.ToStringAndClear();

        Add(type, impact, message, json, players);
    }

    public override void Add(LogType type, ref LogStringHandler handler)
    {
        Add(type, LogImpact.Medium, ref handler);
    }

    public async Task<List<SharedAdminLog>> All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null)
    {
        if (TrySearchCache(filter, out var results))
        {
            return results;
        }

        var initialSize = Math.Min(filter?.Limit ?? 0, 1000);
        List<SharedAdminLog> list;
        if (listProvider != null)
        {
            list = listProvider();
            list.EnsureCapacity(initialSize);
        }
        else
        {
            list = new List<SharedAdminLog>(initialSize);
        }

        await foreach (var log in _db.GetAdminLogs(filter).WithCancellation(filter?.CancellationToken ?? default))
        {
            list.Add(log);
        }

        return list;
    }

    public IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> AllJson(LogFilter? filter = null)
    {
        return _db.GetAdminLogsJson(filter);
    }

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public Task<List<SharedAdminLog>> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllJson(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(_currentRoundId);
    }

    public Task<int> CountLogs(int round)
    {
        return _db.CountAdminLogs(round);
    }
}
