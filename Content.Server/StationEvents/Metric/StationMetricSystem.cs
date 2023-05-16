using Content.Server.Construction.Completions;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.chaos;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Metric;

public class StationMetricSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<T, CalculateChaosEvent>(OnCalculateChaos);
    }

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
            _entities.DeleteEntity(uid);
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

    // /// <summary>
    // /// Called when the gamerule is added
    // /// </summary>
    // protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    // {
    //
    // }

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
