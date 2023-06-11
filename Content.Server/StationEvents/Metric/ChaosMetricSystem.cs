using Content.Server.GameTicking;
using Content.Server.StationEvents.Metric.Components;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Baseclass for systems which measure chaos.
///   Chaos (in ChaosMetrics) is used by the GameDirector to decide which event should run next
///   Subclasses can either calculate chaos in that instant or subscribe to events to track state
///   over time in their component.
/// </summary>
public abstract class ChaosMetricSystem<T> : EntitySystem where T : Component
{
    public abstract ChaosMetrics CalculateChaos(EntityUid uid, T component, ChaosMetricComponent metric, CalculateChaosEvent args);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, CalculateChaosEvent>(OnCalculateChaos);
        SubscribeLocalEvent<T, AddMetric>(OnAdded);
    }

    private void OnCalculateChaos(EntityUid uid, T component, ref CalculateChaosEvent args)
    {
        if (!TryComp<ChaosMetricComponent>(uid, out var metric))
            return;

        var ourChaos = CalculateChaos(uid, component, metric, args);

        // TODO: Some multipliers here based on ChaosMetricComponent ?

        args.Metrics += ourChaos;
    }

    private void OnAdded(EntityUid uid, T component, AddMetric args)
    {
        if (!TryComp<ChaosMetricComponent>(uid, out var metric))
            return;

        Added(uid, component, metric, args);
    }

    /// <summary>
    ///   Called when the metric is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, ChaosMetricComponent metric, AddMetric args)
    {

    }
}
