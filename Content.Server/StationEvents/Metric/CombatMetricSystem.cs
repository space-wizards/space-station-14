using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Zombies;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measures the strength of friendies and hostiles. Also calculates related health / death stats.
///
///   I've used 10 points per entity because later we might somehow estimate combat strength
///   as a multiplier. We could for instance detect damage delt / recieved and look also at
///   entity hitpoints & resistances as an analogue for danger.
///
///   Writes the following
///   Friend : -10 per each friendly entity on the station (negative is GOOD in chaos)
///   Hostile : about 10 points per hostile (those with antag roles) - varies per constants
///   Combat: friendlies + hostiles (to represent the balance of power)
///   Death: 20 per dead body,
///   Medical: 10 for crit + 0.05 * damage (so 5 for 100 damage),
/// </summary>
public sealed class CombatMetricSystem : ChaosMetricSystem<CombatMetricComponent>
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, CombatMetricComponent component,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, DamageableComponent, TransformComponent>();
        var hostiles = FixedPoint2.Zero;
        var friendlies = FixedPoint2.Zero;

        var medical = FixedPoint2.Zero;
        var death = FixedPoint2.Zero;

        var nukieQ = GetEntityQuery<NukeOperativeComponent>();
        var zombieQ = GetEntityQuery<ZombieComponent>();
        var powerQ = GetEntityQuery<CombatPowerComponent>();

        // var humanoidQ = GetEntityQuery<HumanoidAppearanceComponent>();
        var stationGrids = _stationSystem.GetAllStationGrids();

        while (query.MoveNext(out var uid, out var mind, out var mobState, out var damage, out var transform))
        {
            // Don't count anything that is mindless
            if (mind.Mind == null)
                continue;

            // Only count threats currently on station, which avoids salvage threats getting counted for instance.
            // Note this means for instance Nukies on nukie planet don't count, so the threat will spike when they arrive.
            if (transform.GridUid == null || !stationGrids.Contains(transform.GridUid.Value))
                continue;

            // Read per-entity scaling factor (for instance space dragon has much higher threat)
            powerQ.TryGetComponent(uid, out var power);
            var threatMultiple = power?.Threat ?? 1.0f;

            if (_roles.MindIsAntagonist(mind.Mind))
            {
                if (mobState.CurrentState != MobState.Alive)
                    continue;

                // This is an antag
                if (nukieQ.TryGetComponent(uid, out var nukie))
                {
                    hostiles += (component.HostileScore + component.NukieScore) * threatMultiple;
                }
                else if (zombieQ.TryGetComponent(uid, out var zombie))
                {
                    hostiles += (component.HostileScore + component.ZombieScore) * threatMultiple;
                }
                else
                {
                    hostiles += component.HostileScore * threatMultiple;
                }
            }
            else
            {
                // This is a friendly
                if (mobState.CurrentState == MobState.Dead)
                {
                    death += component.DeadScore;
                    continue;
                }
                else
                {
                    medical += damage.Damage.Total * component.MedicalMultiplier;
                    if (mobState.CurrentState == MobState.Critical)
                    {
                        medical += component.CritScore;
                        continue;
                    }
                }

                // Friendlies are good, so make a negative chaos score
                friendlies -= component.FriendlyScore * threatMultiple;
            }
        }

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Friend, friendlies},
            {ChaosMetric.Hostile, hostiles},

            {ChaosMetric.Death, death},
            {ChaosMetric.Medical, medical},
        });
        return chaos;
    }
}
