using System.Text.Json;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameplayMetrics;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.GameplayMetrics;

public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // basic record metric (nothing but name)
    // "advanced" (not really advanced) but adds stuff like round id, round time, gamemode, etc...
    // or have an enum with a bunch of settings you can control? That might be nicer

    public override void RecordMetric(string name, Dictionary<string, string?> metaData, ExtraInfo extraInfo = ExtraInfo.Basic)
    {
        // todo: cache this
        if (!_cfg.GetCVar(CCVars.MetricLoggingEnabled))
            return;

        if (!metaData.TryAdd("name", name))
            throw new Exception("used reserve word name");

        AddExtraAndCheckInformation(metaData, extraInfo);

        var jsonDoc = JsonSerializer.SerializeToDocument(metaData);
        if (jsonDoc == null)
            throw new Exception();

        // TODO: cache the server name
        _db.RecordGameplayMetric(_cfg.GetCVar(CCVars.MetricsServerName), jsonDoc);
    }

    private void AddExtraAndCheckInformation(Dictionary<string, string?> metaData, ExtraInfo extraInfo)
    {
        if (metaData.ContainsKey(ExtraInfo.RoundNumber.ToString()))
            throw new Exception("used reserve word");
        if (extraInfo.HasFlag(ExtraInfo.RoundNumber))
            metaData.Add(ExtraInfo.RoundNumber.ToString(), _ticker.RoundId.ToString());

        if (metaData.ContainsKey(ExtraInfo.GameTime.ToString()))
            throw new Exception("used reserve word");
        if (extraInfo.HasFlag(ExtraInfo.GameTime))
            metaData.Add(ExtraInfo.GameTime.ToString(), _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan).ToString());
    }
}
