using Content.Server.StationEvents.Metric.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Roles;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measure friendly crew's hunger and thirst
///
///   Hunger - 2 points for peckish or 5 for starving per player
///   Thirst - 2 points for thirsty or 5 for parched per player
/// </summary>
public sealed class FoodMetric : ChaosMetricSystem<FoodMetricComponent>
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, FoodMetricComponent component, ChaosMetricComponent metric,
        CalculateChaosEvent args)
    {
        // Add up the pain of all the puddles
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent>();
        var hungerSc = FixedPoint2.Zero;
        var thirstSc = FixedPoint2.Zero;

        var thirstQ = GetEntityQuery<ThirstComponent>();
        var hungerQ = GetEntityQuery<HungerComponent>();

        while (query.MoveNext(out var uid, out var mindContainer, out var mobState))
        {
            // Don't count anything that is mindless, not too worried about feeding enemies
            if (_roles.MindIsAntagonist(mindContainer.Mind))
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
