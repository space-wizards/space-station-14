using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measures the number and severity of anomalies on the station.
///
///   Writes this to the Anomaly chaos value.
/// </summary>
public sealed class AnomalyMetric : ChaosMetricSystem<AnomalyMetricComponent>
{
    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, AnomalyMetricComponent component,
        CalculateChaosEvent args)
    {
        var anomalyChaos = FixedPoint2.Zero;

        // Consider each anomaly and add its stability and growth to the accumulator
        var anomalyQ = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQ.MoveNext(out var uid, out var anomaly))
        {
            if (anomaly.Severity > 0.8f)
            {
                anomalyChaos += component.SeverityCost;
            }
            if (anomaly.Stability > anomaly.GrowthThreshold)
            {
                anomalyChaos += component.GrowingCost;
            }

            anomalyChaos += component.BaseCost;
        }

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Anomaly, anomalyChaos},
        });
        return chaos;
    }
}
