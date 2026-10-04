using System.Numerics;
using Content.Shared.Conditions.Satisfier;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyComponentsCondition : ICondition,
    IConditionWithDefaultSatisfactionRule
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
        return new WithThreshold
        {
            Comparison = WithThreshold.Comparator.GreaterEqual,
            Threshold = 1,
        };
    }
}

/// <summary>
/// Checks if an entity is in range of a specified number of entities with specific components.
/// </summary>
public sealed partial class NearbyComponentsConditionSystem : ConditionEvaluatorSystem<INearbyComponentsCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override float Evaluate(INearbyComponentsCondition condition,
        EntityUid entityUid,
        EntityUid? sourceEntity = null)
    {
        if (condition.Count == 0 || !TryComp(entityUid, out TransformComponent? transformComponent))
            return 0;
        var worldPos = _transform.GetWorldPosition(entityUid);
        var count = 0.0f;


        var box = Box2.CenteredAround(worldPos, new Vector2(condition.Range));

        foreach (var ent in _lookup.GetEntitiesIntersecting(transformComponent.MapID, box))
        {
            if (condition.Anchored != transformComponent.Anchored)
                continue;

            foreach (var compType in condition.Components.Values)
            {
                if (!HasComp(ent, compType.Component.GetType()))
                    continue;
                count++;
            }
        }

        return count / condition.Count;
    }
}
