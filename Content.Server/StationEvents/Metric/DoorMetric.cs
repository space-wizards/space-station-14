using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Uses doors and firelocks to sample station chaos across the station
///
///   Emag - 10 points per emaged door
///   Power - 5 points per door or firelock with no power
///   Atmos - 10 points for holding spacing or 20 for holding back fire
/// </summary>
public sealed class DoorMetric : ChaosMetricSystem<DoorMetricComponent>
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

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
