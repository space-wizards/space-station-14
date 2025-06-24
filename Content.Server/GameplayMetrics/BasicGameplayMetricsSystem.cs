using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.GameplayMetrics;

namespace Content.Server.GameplayMetrics;

public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void RecordMetric(string campaign, string metadata)
    {
        _db.AddTelemetryData(_ticker.RoundId, campaign, metadata);
    }
}
