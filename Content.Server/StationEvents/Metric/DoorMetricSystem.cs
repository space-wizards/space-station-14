using Content.Server.Power.Components;
using Content.Server.Station.Systems;
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
public sealed class DoorMetricSystem : ChaosMetricSystem<DoorMetricComponent>
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, DoorMetricComponent component,
        CalculateChaosEvent args)
    {

        var firelockQ = GetEntityQuery<FirelockComponent>();
        var airlockQ = GetEntityQuery<AirlockComponent>();

        // Keep counters to calculate average at the end.
        var doorCounter = FixedPoint2.Zero;
        var firelockCounter = FixedPoint2.Zero;
        var airlockCounter = FixedPoint2.Zero;

        var fireCount = FixedPoint2.Zero;
        var pressureCount = FixedPoint2.Zero;
        var emagCount = FixedPoint2.Zero;
        var powerCount = FixedPoint2.Zero;

        // Add up the pain of all the doors
        // Restrict to just doors on the main station
        var stationGrids = _stationSystem.GetAllStationGrids();

        var queryFirelock = EntityQueryEnumerator<DoorComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (queryFirelock.MoveNext(out var uid, out var door, out var power, out var transform))
        {
            if (transform.GridUid == null || !stationGrids.Contains(transform.GridUid.Value))
                continue;

            if (firelockQ.TryGetComponent(uid, out var firelock))
            {
                if (firelock.DangerFire)
                {
                    fireCount += 1;
                }
                else if (firelock.DangerPressure)
                {
                    pressureCount += 1;
                }

                firelockCounter += 1;
            }

            if (airlockQ.TryGetComponent(uid, out var airlock))
            {
                if (door.State == DoorState.Emagging)
                {
                    emagCount += 1;
                }

                airlockCounter += 1;
            }

            if (!power.PoweredLastUpdate ?? true)
            {
                powerCount += 1;
            }

            doorCounter += 1;
        }

        // Calculate each stat as a fraction of all doors in the station.
        //   That way the metrics do not "scale up" on large stations.
        var emagChaos = (emagCount / airlockCounter) * component.EmagCost;
        var atmosChaos = (fireCount / firelockCounter) * component.FireCost +
                         (pressureCount / firelockCounter) * component.PressureCost;
        var powerChaos = (powerCount / doorCounter) * component.PowerCost;

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Security, emagChaos},
            {ChaosMetric.Atmos, atmosChaos},
            {ChaosMetric.Power, powerChaos},
        });
        return chaos;
    }
}
