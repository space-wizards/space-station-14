using System.Text.Json;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameplayMetrics;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameplayMetrics;

public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // ccvars
    private bool _enabled;
    private string _serverName = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.BasicMetricLoggingEnabled, x => _enabled = x, true);
        _cfg.OnValueChanged(CCVars.BasicMetricsServerName, x => _serverName = x, true);
    }

    public override void RecordMetric(string name, Dictionary<string, object?> logData, ExtraInfo extraInfo = ExtraInfo.Basic)
    {
        if (_enabled)
            return;

        if (!logData.TryAdd("name", name))
            throw new DebugAssertException("You can't use reserved word \"name\" in log data.");

        AddExtraAndCheckInformation(logData, extraInfo);

        var jsonDoc = JsonSerializer.SerializeToDocument(logData);

        _db.RecordGameplayMetric(_serverName, jsonDoc);
    }

    private void AddExtraAndCheckInformation(Dictionary<string, object?> metaData, ExtraInfo extraInfo)
    {
        var roundNumber = ExtraInfo.RoundNumber.ToString();
        if (metaData.ContainsKey(roundNumber))
            throw new DebugAssertException($"You can't use reserved word \"{roundNumber}\" in log data.");
        if (extraInfo.HasFlag(ExtraInfo.RoundNumber))
            metaData.Add(roundNumber, _ticker.RoundId.ToString());

        var gameTime = ExtraInfo.GameTime.ToString();
        if (metaData.ContainsKey(gameTime))
            throw new DebugAssertException($"You can't use reserved word \"{gameTime}\" in log data.");
        if (extraInfo.HasFlag(ExtraInfo.GameTime))
            metaData.Add(gameTime, _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan).ToString());
    }
}
