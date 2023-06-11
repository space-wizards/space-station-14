using Content.Server.Chemistry.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.StationEvents.Metric.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Zombies;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measure friendly crew's hunger and thirst
///
///   Hunger - 2 points for peckish or 5 for starving per player
///   Thirst - 2 points for thirsty or 5 for parched per player
/// </summary>
public sealed class FoodMetric : ChaosMetricSystem<FoodMetricComponent>
{
    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, FoodMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindComponent, MobStateComponent>();
        FixedPoint2 hungerSc = 0.0f;
        FixedPoint2 thirstSc = 0.0f;

        var thirstQ = GetEntityQuery<ThirstComponent>();
        var hungerQ = GetEntityQuery<HungerComponent>();

        while (query.MoveNext(out var uid, out var mind, out var mobState))
        {
            // Don't count anything that is mindless
            if (mind.Mind == null)
                continue;

            // Not too worried about feeding enemies
            if (mind.Mind.HasAntag)
                continue;

            if (thirstQ.TryGetComponent(uid, out var thirst))
            {
                if (thirst.CurrentThirstThreshold == ThirstThreshold.Thirsty)
                {
                    thirstSc += component.ThirstScore;
                }
                else if (thirst.CurrentThirstThreshold == ThirstThreshold.Parched)
                {
                    thirstSc += component.ParchedScore;
                }
            }

            if (hungerQ.TryGetComponent(uid, out var hunger))
            {
                if (hunger.CurrentThreshold == HungerThreshold.Peckish)
                {
                    hungerSc += component.PeckishScore;
                }
                if (hunger.CurrentThreshold == HungerThreshold.Starving)
                {
                    hungerSc += component.StarvingScore;
                }
            }
        }

        var chaos = new ChaosMetrics(new Dictionary<string, FixedPoint2>()
        {
            {"Hunger", hungerSc},
            {"Thirst", thirstSc},
        });
        return chaos;
    }
}
