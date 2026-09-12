using Content.Server.Chemistry.EntitySystems;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measure the mess of the station in puddles on the floor
///
///   Jani - JaniMetricComponent.Puddles points per BaselineQty of various substances
/// </summary>
public sealed partial class PuddleMetricSystem : ChaosMetricSystem<PuddleMetricComponent>
{
    [Dependency] private SolutionContainerSystem _solutionContainerSystem = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, PuddleMetricComponent component,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<PuddleComponent>();
        var mess = FixedPoint2.Zero;
        while (query.MoveNext(out var puddleUid, out var puddle))
        {
            if (!_solutionContainerSystem.ResolveSolution(puddleUid, puddle.SolutionName, ref puddle.Solution, out var puddleSolution))
                continue;

            FixedPoint2 puddleChaos = 0.0f;
            foreach (var substance in puddleSolution.Contents)
            {
                FixedPoint2 substanceChaos = component.Puddles.GetValueOrDefault(substance.Reagent.Prototype, component.PuddleDefault);
                puddleChaos += substanceChaos * substance.Quantity;
            }

            mess += puddleChaos;
        }

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Mess, mess}
        });
        return chaos;
    }
}
