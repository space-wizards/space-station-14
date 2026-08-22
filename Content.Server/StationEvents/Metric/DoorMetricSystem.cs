using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Uses doors and firelocks to sample station chaos across the station
///
///   Emag - EmagCost per emaged door
///   Power - PowerCost per door or firelock with no power
///   Atmos - PressureCost for holding spacing or FireCost for holding back fire
/// </summary>
public sealed partial class DoorMetricSystem : ChaosMetricSystem<DoorMetricComponent>
{
    [Dependency] private StationSystem _stationSystem = default!;

    private HashSet<EntityUid> _emaggedDoors = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, GotEmaggedEvent>(OnDoorEmagged);
    }

    private void OnDoorEmagged(EntityUid uid, AirlockComponent airlock, ref GotEmaggedEvent args)
    {
        _emaggedDoors.Add(uid);
    }

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, DoorMetricComponent component,
        CalculateChaosEvent args)
    {
        var firelockQ = GetEntityQuery<FirelockComponent>();
        var airlockQ = GetEntityQuery<AirlockComponent>();

        var boltQ = GetEntityQuery<DoorBoltComponent>();

        // Keep counters to calculate average at the end.
        var doorCounter = FixedPoint2.Zero;
        var firelockCounter = FixedPoint2.Zero;
        var airlockCounter = FixedPoint2.Zero;

        var fireCount = FixedPoint2.Zero;
        var pressureCount = FixedPoint2.Zero;
        var emagCount = FixedPoint2.Zero;
        var powerCount = FixedPoint2.Zero;
        var openBoltedCount = FixedPoint2.Zero;

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
                if (firelock.Temperature)
                {
                    fireCount += 1;
                }
                else if (firelock.Pressure)
                {
                    pressureCount += 1;
                }

                firelockCounter += 1;
            }

            if (airlockQ.TryGetComponent(uid, out var airlock))
            {
                if (_emaggedDoors.Contains(uid))
                {
                    emagCount += 1;
                }

                if (door.State == DoorState.Open
                    && boltQ.TryGetComponent(uid, out var bolts)
                    && bolts.BoltsDown)
                {
                    openBoltedCount += 1;
                }

                airlockCounter += 1;
            }

            if (!power.Powered)
            {
                powerCount += 1;
            }

            doorCounter += 1;
        }

        // Calculate each stat as a fraction of all doors in the station.
        //   That way the metrics do not "scale up" on large stations.
        var emagChaos = airlockCounter > 0
            ? (emagCount / airlockCounter) * component.EmagCost
            : FixedPoint2.Zero;
        var openBoltedChaos = airlockCounter > 0
            ? (openBoltedCount / airlockCounter) * component.OpenBoltedCost
            : FixedPoint2.Zero;
        var atmosChaos = firelockCounter > 0
            ? (fireCount / firelockCounter) * component.FireCost +
              (pressureCount / firelockCounter) * component.PressureCost
            : FixedPoint2.Zero;
        var powerChaos = doorCounter > 0
            ? (powerCount / doorCounter) * component.PowerCost
            : FixedPoint2.Zero;

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Security, emagChaos + openBoltedChaos},
            {ChaosMetric.Atmos, atmosChaos},
            {ChaosMetric.Power, powerChaos},
        });
        return chaos;
    }
}
