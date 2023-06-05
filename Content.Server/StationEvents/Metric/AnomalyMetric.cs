using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

public sealed class AnomalyMetric : StationMetric<AnomalyMetricComponent>
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, AnomalyMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        FixedPoint2 anomalyChaos = 0.0f;

        // Add up the pain of all the firelocks
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

        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>()
        {
            {"Anomaly", anomalyChaos},
        });
        return chaos;
    }
}
