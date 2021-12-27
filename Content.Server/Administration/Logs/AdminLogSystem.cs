using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Prometheus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Content.Server.Administration.Logs;

public partial class AdminLogSystem : SharedAdminLogSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    [Dependency] private readonly GameTicker _gameTicker = default!;

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

    // Per update
    private float _accumulatedFrameTime;
    private readonly ConcurrentQueue<QueuedLog> _logQueue = new();
    private readonly ConcurrentQueue<QueuedLog> _preRoundLogQueue = new();

    // Per round
    private int CurrentRoundId => _gameTicker.RoundId;
    private int _currentLogId;
    private int NextLogId => Interlocked.Increment(ref _currentLogId);

    public override void Initialize()
    {
        base.Initialize();

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

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Set(0);
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }

        SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
    }

    public override async void Shutdown()
    {
        base.Shutdown();

        if (!_logQueue.IsEmpty)
        {
            await SaveLogs();
        }
    }

    public override async void Update(float frameTime)
    {
        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
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

        if (_accumulatedFrameTime >= _queueSendDelay.TotalSeconds)
        {
            await SaveLogs();
            return;
        }

        if (count >= _queueMax)
        {
            if (_metricsEnabled)
            {
                QueueCapReached.Inc();
            }

            _sawmill.Warning($"Maximum cap of {_queueMax} reached for admin logs.");
            await SaveLogs();
            return;
        }

        _accumulatedFrameTime += frameTime;
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

        _sawmill.Warning($"Maximum cap of {_preRoundQueueMax} reached for pre-round admin logs.");
        await SaveLogs();
    }

    private async Task SaveLogs()
    {
        _accumulatedFrameTime = 0;

        // TODO ADMIN LOGS array pool
        var copy = new List<QueuedLog>(_logQueue.Count + _preRoundLogQueue.Count);

        copy.AddRange(_logQueue);
        _logQueue.Clear();
        Queue.Set(0);

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            _sawmill.Error($"Dropping {_preRoundLogQueue.Count} pre-round logs. Current cap: {_preRoundQueueMax}");
        }
        else
        {
            foreach (var queued in _preRoundLogQueue)
            {
                queued.Log.RoundId = _gameTicker.RoundId;
                CacheLog(queued);
            }

            copy.AddRange(_preRoundLogQueue);
        }

        _preRoundLogQueue.Clear();
        PreRoundQueue.Set(0);

        // ship the logs to Azkaban
        var task = Task.Run(async () =>
        {
            await _db.AddAdminLogs(copy);
        });

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

    private void RoundStarting(RoundStartingEvent ev)
    {
        Interlocked.Exchange(ref _currentLogId, 0);
        CacheNewRound();

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Set(0);
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    private async void Add(LogType type, LogImpact impact, string message, JsonDocument json, HashSet<Guid> players, Dictionary<int, string?> entities)
    {
        var logId = NextLogId;
        var date = DateTime.UtcNow;

        var log = new AdminLog
        {
            Id = logId,
            RoundId = CurrentRoundId,
            Type = type,
            Impact = impact,
            Date = date,
            Message = message,
            Json = json,
            Players = new List<AdminLogPlayer>(players.Count)
        };

        var queued = new QueuedLog(log, entities);

        foreach (var id in players)
        {
            var player = new AdminLogPlayer
            {
                LogId = logId,
                PlayerUserId = id
            };

            log.Players.Add(player);
        }

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            _preRoundLogQueue.Enqueue(queued);
        }
        else
        {
            _logQueue.Enqueue(queued);
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

        var (json, players, entities) = ToJson(handler.Values);
        var message = handler.ToStringAndClear();

        Add(type, impact, message, json, players, entities);
    }

    public override void Add(LogType type, ref LogStringHandler handler)
    {
        Add(type, LogImpact.Medium, ref handler);
    }

    public async Task<List<SharedAdminLog>> All(LogFilter? filter = null)
    {
        if (TrySearchCache(filter, out var results))
        {
            return results;
        }

        var initialSize = Math.Min(filter?.Limit ?? 0, 1000);
        var list = new List<SharedAdminLog>(initialSize);

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
        filter.Round = CurrentRoundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = CurrentRoundId;
        return AllMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = CurrentRoundId;
        return AllJson(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(CurrentRoundId);
    }
}
