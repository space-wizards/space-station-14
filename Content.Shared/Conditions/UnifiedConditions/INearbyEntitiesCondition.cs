using Content.Shared.Conditions.Satisfier;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyEntitiesCondition : ICondition,
    IConditionWithDefaultSatisfactionRule
{
    int Count { get; }

    EntityWhitelist Whitelist { get; }

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
/// Checks for entities matching the whitelist in range.
/// </summary>
public sealed partial class NearbyEntitiesConditionSystem : ConditionEvaluatorSystem<INearbyEntitiesCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override float Evaluate(INearbyEntitiesCondition condition,
        EntityUid entityUid,
        EntityUid? sourceEntity = null)
    {
        if (condition.Count == 0 || !TryComp(entityUid, out TransformComponent? transform))
            return 0;

        if (transform.MapUid == null)
            return 0;

        var worldPos = _transform.GetWorldPosition(transform);

        float value = 0;
        foreach (var ent in _lookup.GetEntitiesInRange(transform.MapID, worldPos, condition.Range))
        {
            if (_whitelist.IsWhitelistFail(condition.Whitelist, ent))
                continue;

            value++;
        }

        return value / condition.Count;
    }
}
