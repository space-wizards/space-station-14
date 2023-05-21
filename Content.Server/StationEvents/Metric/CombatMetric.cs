using Content.Server.Chemistry.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.chaos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;

namespace Content.Server.StationEvents.Metric;

public sealed class CombatMetric : StationMetric<CombatMetricComponent>
{
    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, CombatMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindComponent, MobStateComponent, DamageableComponent>();
        FixedPoint2 hostiles = 0.0f;
        FixedPoint2 friendlies = 0.0f;

        FixedPoint2 medical = 0.0f;
        FixedPoint2 death = 0.0f;

        var nukieQ = GetEntityQuery<NukeOperativeComponent>();
        var zombieQ = GetEntityQuery<ZombieComponent>();

        var humanoidQ = GetEntityQuery<HumanoidAppearanceComponent>();

        while (query.MoveNext(out var uid, out var mind, out var mobState, out var damage))
        {
            // Don't count anything that is mindless
            if (mind.Mind == null)
                continue;

            if (mind.Mind.HasAntag)
            {
                if (mobState.CurrentState != MobState.Alive)
                    continue;

                // This is an antag
                if (nukieQ.TryGetComponent(uid, out var nukie))
                {
                    hostiles += component.HostileScore + component.NukieScore;
                }
                else if (zombieQ.TryGetComponent(uid, out var zombie))
                {
                    hostiles += component.HostileScore + component.ZombieScore;
                }
                else
                {
                    hostiles += component.HostileScore;
                }
            }
            else
            {
                // This is a friendly
                // Quick filter for non-pets
                if (!humanoidQ.TryGetComponent(uid, out var humanoid))
                {
                    continue;
                }

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
                friendlies -= component.FriendlyScore;

            }
        }

        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>()
        {
            {"Friend", friendlies},
            {"Hostile", hostiles},
            {"Combat", friendlies + hostiles},

            {"Death", death},
            {"Medical", medical},
        });
        return chaos;
    }
}
