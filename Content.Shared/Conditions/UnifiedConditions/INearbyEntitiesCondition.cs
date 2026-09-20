using Content.Shared.Conditions.Satisfier;
using Content.Shared.Whitelist;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyEntitiesCondition : ICondition<INearbyEntitiesCondition>, IConditionWithDefaultSatisfactionRule
{

    int Count { get; }

    EntityWhitelist Whitelist { get; }

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
/// Checks for entities matching the whitelist in range.
/// </summary>
public sealed partial class NearbyEntitiesConditionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<INearbyEntitiesCondition> args)
    {
        args.Handled = true;

        if (entity.Comp.MapUid == null)
        {
            return;
        }

        var worldPos = _transform.GetWorldPosition(entity.Comp);

        foreach (var ent in _lookup.GetEntitiesInRange(entity.Comp.MapID, worldPos, args.Condition.Range))
        {
            if (_whitelist.IsWhitelistFail(args.Condition.Whitelist, ent))
                continue;

            args.Value++;
        }

        args.Value /= args.Condition.Count;

    }
}
