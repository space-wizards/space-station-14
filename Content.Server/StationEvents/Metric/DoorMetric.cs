using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

public sealed class DoorMetric : StationMetric<DoorMetricComponent>
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    // public override void Initialize()
    // {
    //     base.Initialize();
    // }

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, DoorMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {

        FixedPoint2 powerChaos = 0.0f;
        FixedPoint2 atmosChaos = 0.0f;
        FixedPoint2 emagChaos = 0.0f;

        // Add up the pain of all the firelocks
        var queryFirelock = EntityQueryEnumerator<DoorComponent, FirelockComponent, ApcPowerReceiverComponent>();
        while (queryFirelock.MoveNext(out var uid, out var door, out var firelock, out var power))
        {
            if (firelock.DangerFire)
            {
                atmosChaos += component.FireCost;
            }
            else if (firelock.DangerPressure)
            {
                atmosChaos += component.PressureCost;
            }

            if (!power.PoweredLastUpdate ?? true)
            {
                powerChaos += component.PowerCost;
            }
        }

        var queryDoor = EntityQueryEnumerator<DoorComponent, AirlockComponent, ApcPowerReceiverComponent>();
        while (queryDoor.MoveNext(out var uid, out var door, out var airlock, out var power))
        {
            if (door.State == DoorState.Emagging)
            {
                emagChaos += component.EmagCost;
            }

            if (!power.PoweredLastUpdate ?? true)
            {
                powerChaos += component.PowerCost;
            }
        }

        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>()
        {
            {"Emag", emagChaos},
            {"Atmos", atmosChaos},
            {"Power", powerChaos},
        });
        return chaos;
    }
}
