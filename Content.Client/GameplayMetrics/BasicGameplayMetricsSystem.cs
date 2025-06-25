using Content.Shared.GameplayMetrics;

namespace Content.Client.GameplayMetrics;

public sealed class BasicGameplayMetricsSystem : SharedBasicGameplayMetricsSystem
{
    public override void RecordMetric(string name, Dictionary<string, object?> metricData, ExtraInfo extraInfo = ExtraInfo.Basic)
    {
        // no op
    }
}
