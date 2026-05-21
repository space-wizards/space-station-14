using Content.Server.StationEvents.Metric.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Roles;

namespace Content.Server.StationEvents.Metric;

/// <summary>
///   Measure crew's hunger and thirst
///
/// </summary>
public sealed class FoodMetricSystem : ChaosMetricSystem<FoodMetricComponent>
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override ChaosMetrics CalculateChaos(EntityUid metric_uid, FoodMetricComponent component,
        CalculateChaosEvent args)
    {
        // Gather hunger and thirst scores
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent>();
        var hungerSc = FixedPoint2.Zero;
        var thirstSc = FixedPoint2.Zero;

        var thirstQ = GetEntityQuery<ThirstComponent>();
        var hungerQ = GetEntityQuery<HungerComponent>();

        while (query.MoveNext(out var uid, out var mindContainer, out var mobState))
        {
            // Don't count anything that is mindless, do count antags
            if (mindContainer.Mind == null)
                continue;

            if (mobState.CurrentState != MobState.Alive)
                continue;

            if (thirstQ.TryGetComponent(uid, out var thirst))
            {
                thirstSc += component.ThirstScores.GetValueOrDefault(thirst.CurrentThirstThreshold);
            }

            if (hungerQ.TryGetComponent(uid, out var hunger))
            {
                hungerSc += component.HungerScores.GetValueOrDefault(hunger.CurrentThreshold);
            }
        }

        var chaos = new ChaosMetrics(new Dictionary<ChaosMetric, FixedPoint2>()
        {
            {ChaosMetric.Hunger, hungerSc},
            {ChaosMetric.Thirst, thirstSc},
        });
        return chaos;
    }
}
