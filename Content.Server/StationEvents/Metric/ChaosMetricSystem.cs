using Content.Server.GameTicking;
using Content.Server.Station.Systems;
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
    public abstract ChaosMetrics CalculateChaos(EntityUid uid, T component, CalculateChaosEvent args);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, CalculateChaosEvent>(OnCalculateChaos);
    }

    private void OnCalculateChaos(EntityUid uid, T component, ref CalculateChaosEvent args)
    {
        var ourChaos = CalculateChaos(uid, component, args);

        args.Metrics += ourChaos;
    }

}
