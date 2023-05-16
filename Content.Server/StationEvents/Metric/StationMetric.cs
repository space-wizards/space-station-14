using Content.Server.GameTicking;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.chaos;

namespace Content.Server.StationEvents.Metric;

public abstract class StationMetric<T> : EntitySystem where T : Component
{
    // [Dependency] protected readonly GameTicker GameTicker = default!;

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
    /// Called when the metric is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, ChaosMetricComponent metric, AddMetric args)
    {

    }

    // /// <summary>
    // /// Called on an active gamerule entity in the Update function
    // /// </summary>
    // protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    // {
    //
    // }
    //
    // public override void Update(float frameTime)
    // {
    //     base.Update(frameTime);
    //
    //     var query = EntityQueryEnumerator<T, GameRuleComponent>();
    //     while (query.MoveNext(out var uid, out var comp1, out var comp2))
    //     {
    //         if (!GameTicker.IsGameRuleActive(uid, comp2))
    //             continue;
    //
    //         ActiveTick(uid, comp1, comp2, frameTime);
    //     }
    // }
}
