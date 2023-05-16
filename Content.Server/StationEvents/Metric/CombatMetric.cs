using Content.Server.Chemistry.EntitySystems;
using Content.Server.Mind.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.chaos;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

public sealed class CombatMetric : StationMetric<CombatMetricComponent>
{
    // public override void Initialize()
    // {
    //     base.Initialize();
    // }

    public override ChaosMetrics CalculateChaos(EntityUid uid, CombatMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>(){{"Jani", 0.0f}});

        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var entity, out var mind))
        {
            if (mind.Mind == null)
            {
                // Don't count anything that is mindless
                continue;
            }

            bool antag = mind.Mind.HasAntag;


            // See PricingSystem?
            // Add a threat component for each mob?

            chaos.ChaosDict["Jani"] += puddleChaos / component.baselineQty;
        }

        return chaos;
    }
}
