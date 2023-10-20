using Content.Server.Power.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Uses doors and firelocks to sample station chaos across the station
///
///   Emag - EmagCost per emaged door
///   Power - PowerCost per door or firelock with no power
///   Atmos - PressureCost for holding spacing or FireCost for holding back fire
/// </summary>
public sealed class DoorMetric : ChaosMetricSystem<DoorMetricComponent>
{
    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, DoorMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {

        var powerChaos = FixedPoint2.Zero;
        var atmosChaos = FixedPoint2.Zero;
        var emagChaos = FixedPoint2.Zero;

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
