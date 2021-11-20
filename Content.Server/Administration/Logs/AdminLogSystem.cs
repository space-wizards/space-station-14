using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs.Converters;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Shared.Administration.Logs;
using Prometheus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Logs;

public class AdminLogSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private static readonly TimeSpan QueueSendThreshold = TimeSpan.FromSeconds(5);
    private const int QueueMaxLogs = 5000;

    private static readonly Histogram DatabaseUpdateTime = Metrics.CreateHistogram(
        "admin_logs_database_time",
        "Time used to send logs to the database in ms",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.5, 20)
        });

    private static readonly Gauge QueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the log queue cap has been reached in a round.");

    private static readonly Gauge LogsSent = Metrics.CreateGauge(
        "admin_logs_sent",
        "Amount of logs sent to the database in a round.");

    private ISawmill _log = default!;

    private bool _metricsEnabled;

    private int _roundId;
    private JsonSerializerOptions _jsonOptions = default!;
    private readonly ConcurrentQueue<AdminLog> _logsToAdd = new();

    private float _accumulatedFrameTime;

    public override void Initialize()
    {
        base.Initialize();

        _log = _logManager.GetSawmill("admin.logs");
        _jsonOptions = new JsonSerializerOptions();

        foreach (var converter in _reflection.FindTypesWithAttribute<AdminLogConverterAttribute>())
        {
            var instance = _typeFactory.CreateInstance<JsonConverter>(converter);
            _jsonOptions.Converters.Add(instance);
        }

        var converterNames = _jsonOptions.Converters.Select(converter => converter.GetType().Name);
        _log.Info($"Admin log converters found: {string.Join(" ", converterNames)}");

        _configuration.OnValueChanged(CVars.MetricsEnabled, value => _metricsEnabled = value, true);

        if (_metricsEnabled)
        {
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }

        SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
    }

    public override async void Shutdown()
    {
        base.Shutdown();

        if (!_logsToAdd.IsEmpty)
        {
            await SendLogs();
        }
    }

    public override async void Update(float frameTime)
    {
        var count = _logsToAdd.Count;
        if (count == 0)
        {
            return;
        }

        if (count < QueueMaxLogs && _accumulatedFrameTime < QueueSendThreshold.TotalSeconds)
        {
            _accumulatedFrameTime += frameTime;
            return;
        }

        await SendLogs();
    }

    private async Task SendLogs()
    {
        var copy = new List<AdminLog>(_logsToAdd);
        _logsToAdd.Clear();
        _accumulatedFrameTime = 0;

        // ship the logs to Azkaban
        var task = Task.Run(() =>
        {
            _db.AddAdminLogs(copy);
        });

        if (_metricsEnabled)
        {
            if (copy.Count >= QueueMaxLogs)
            {
                QueueCapReached.Inc();
            }

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
        _roundId = ev.RoundId;

        if (_metricsEnabled)
        {
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    private async void Add(LogType type, string message, JsonDocument json, List<Guid> players)
    {
        var log = new AdminLog
        {
            RoundId = _roundId,
            Type = type,
            Date = DateTime.UtcNow,
            Message = message,
            Json = json,
            Players = new List<AdminLogPlayer>(players.Count)
        };

        _logsToAdd.Enqueue(log);

        foreach (var id in players)
        {
            var player = new AdminLogPlayer
            {
                PlayerId = id,
                RoundId = _roundId
            };

            log.Players.Add(player);
        }
    }

    public void Add(LogType type, ref LogStringHandler handler)
    {
        var (json, players) = handler.ToJson(_jsonOptions, _entities);
        var message = handler.ToStringAndClear();

        Add(type, message, json, players);
    }

    public IAsyncEnumerable<LogRecord> All(LogFilter? filter = null)
    {
        return _db.GetAdminLogs(filter);
    }

    public IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter);
    }

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public IAsyncEnumerable<LogRecord> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _roundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _roundId;
        return AllMessages(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(_roundId);
    }
}
