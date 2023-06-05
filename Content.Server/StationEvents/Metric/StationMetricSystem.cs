using Content.Server.Construction.Completions;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Metric.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Metric;

public class StationMetricSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public Dictionary<EntityPrototype, ChaosMetricComponent> AllMetrics()
    {
        var allMetrics = new Dictionary<EntityPrototype, ChaosMetricComponent>();
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!prototype.TryGetComponent<ChaosMetricComponent>(out var metric))
                continue;

            allMetrics.Add(prototype, metric);
        }

        return allMetrics;
    }

    public void SetupMetrics()
    {
        var metrics = AllMetrics();

        // Erase all the metrics
        //  TODO: Use MetaDataComponent to selectively only load new metrics.
        var query = EntityQueryEnumerator<ChaosMetricComponent>();
        while (query.MoveNext(out var uid, out var metric))
        {
            Del(uid);
        }

        // Set them up again
        foreach (var (proto, metric) in metrics)
        {
            var metricEntity = Spawn(proto.ID, MapCoordinates.Nullspace);
            RaiseLocalEvent(metricEntity, new AddMetric());
        }
    }
    public ChaosMetrics CalculateChaos()
    {
        var calcEvent = new CalculateChaosEvent(new ChaosMetrics());
        var query = EntityQueryEnumerator<ChaosMetricComponent>();
        while (query.MoveNext(out var uid, out var metric))
        {
            RaiseLocalEvent(uid, ref calcEvent);
        }

        return calcEvent.Metrics;
    }
}
