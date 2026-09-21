using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Conditions.Satisfier;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyAccessCondition : ICondition<INearbyAccessCondition>, IConditionWithDefaultSatisfactionRule
{
    // This exists because of door electronics contained inside doors.
    /// <summary>
    /// Does the access entity need to be anchored.
    /// </summary>
    bool Anchored { get; }

    /// <summary>
    /// Count of entities that need to be nearby.
    /// </summary>
    int Count { get; }

    List<ProtoId<AccessLevelPrototype>> Access { get; }

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
/// Checks for a number of entities nearby with the specified accesses.
/// </summary>
public sealed partial class NearbyAccessConditionSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AccessReaderSystem _reader = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<INearbyAccessCondition> args)
    {
        args.Handled = true;

        if (entity.Comp.MapUid == null)
            return;

        var count = 0f;

        foreach (var (ent, comp) in _lookup.GetEntitiesInRange<AccessReaderComponent>(entity.Comp.Coordinates,
                     args.Condition.Range))
        {
            if (!_reader.AreAccessTagsAllowed(args.Condition.Access, comp) ||
                args.Condition.Anchored && !Transform(ent).Anchored)
                continue;
            count++;
        }

        args.Value = count / args.Condition.Count;
    }
}
