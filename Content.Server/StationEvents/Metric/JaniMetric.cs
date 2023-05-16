using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.chaos;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

public sealed class JaniMetric : StationMetric<JaniMetricComponent>
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    // public override void Initialize()
    // {
    //     base.Initialize();
    // }

    public override ChaosMetrics CalculateChaos(EntityUid uid, JaniMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>(){{"Jani", 0.0f}});

        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<PuddleComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var puddleUid, out var puddle, out var solutionMgr))
        {
            if (!_solutionContainerSystem.TryGetSolution(puddleUid, puddle.SolutionName, out var puddleSolution, solutionMgr))
                continue;

            FixedPoint2 puddleChaos = 0.0f;
            foreach (var substance in puddleSolution.Contents)
            {
                FixedPoint2 substanceChaos = component.Puddles.GetValueOrDefault(substance.ReagentId, component.PuddleDefault);
                puddleChaos += substanceChaos * substance.Quantity;
            }

            chaos.ChaosDict["Jani"] += puddleChaos / component.baselineQty;
        }

        return chaos;
    }
}
