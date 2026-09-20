using System.Numerics;
using Content.Shared.Conditions.Satisfier;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyComponentsCondition : ICondition<INearbyComponentsCondition>, IConditionWithDefaultSatisfactionRule
{
    /// <summary>
    /// Does the entity need to be anchored.
    /// </summary>
    bool Anchored { get; }

    int Count { get; }

    ComponentRegistry Components { get; }

    float Range { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithThreshold()
        {
            Comparison = WithThreshold.Comparator.GreaterEqual,
            Threshold = 1,
        };
    }
}

/// <summary>
/// Checks if an entity is in range of a specified number of entities with specific components.
/// </summary>
public sealed partial class NearbyComponentsConditionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<INearbyComponentsCondition> args)
    {
        args.Handled = true;

        var worldPos = _transform.GetWorldPosition(entity.Comp);
        var count = 0.0f;

        var box = Box2.CenteredAround(worldPos, new Vector2(args.Condition.Range));

        foreach (var ent in _lookup.GetEntitiesIntersecting(entity.Comp.MapID, box))
        {
            if (args.Condition.Anchored && !Transform(ent).Anchored)
                continue;

            foreach (var compType in args.Condition.Components.Values)
            {
                if (!HasComp(ent, compType.Component.GetType()))
                    continue;
                count++;
            }
        }

        args.Value = count / args.Condition.Count;
    }
}
