using System.Text.Json;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameplayMetrics;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameplayMetrics;

/// <summary>
/// System for creating basic
/// </summary>
public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // ccvars
    private bool _enabled;
    private string _serverName = string.Empty;

    private const string NameReserve = "name";

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.BasicMetricLoggingEnabled, x => _enabled = x, true);
        _cfg.OnValueChanged(CCVars.BasicMetricsServerName, x => _serverName = x, true);
    }

    /// <summary>
    /// Record a metric to the database.
    /// </summary>
    /// <remarks>There are several reserved words. Specifically "name" and the enums in <see cref="ExtraInfo"/></remarks>
    public override void RecordMetric(string name, Dictionary<string, object?> metricData, ExtraInfo extraInfo = ExtraInfo.Basic)
    {
        if (!_enabled)
            return;

        if (!metricData.TryAdd(NameReserve, name))
            throw new DebugAssertException($"You can't use reserved word \"{NameReserve}\" in log data.");

        AddExtraAndCheckInformation(metricData, extraInfo);

        var jsonDoc = JsonSerializer.SerializeToDocument(metricData);

        _db.RecordGameplayMetric(_serverName, jsonDoc);
    }

    private void AddExtraAndCheckInformation(Dictionary<string, object?> metricData, ExtraInfo extraInfo)
    {
        var roundNumber = nameof(ExtraInfo.RoundNumber);
        if (metricData.ContainsKey(roundNumber))
            throw new DebugAssertException($"You can't use reserved word \"{roundNumber}\" in log data.");
        if (extraInfo.HasFlag(ExtraInfo.RoundNumber))
            metricData.Add(roundNumber, _ticker.RoundId.ToString());

        var gameTime = nameof(ExtraInfo.GameTime);
        if (metricData.ContainsKey(gameTime))
            throw new DebugAssertException($"You can't use reserved word \"{gameTime}\" in log data.");
        if (extraInfo.HasFlag(ExtraInfo.GameTime))
            metricData.Add(gameTime, _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan).ToString());
    }
}
