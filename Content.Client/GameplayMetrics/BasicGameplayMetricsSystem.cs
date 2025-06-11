using Content.Shared.GameplayMetrics;

namespace Content.Client.GameplayMetrics;

public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    public override void RecordMetric(string campaign, string metadata)
    {
        // no op
    }
}
