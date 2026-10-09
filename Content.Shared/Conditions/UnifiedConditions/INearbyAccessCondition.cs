using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Conditions.Satisfier;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface INearbyAccessCondition : ICondition,
    IConditionWithDefaultSatisfactionRule
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
public sealed partial class NearbyAccessConditionSystem : ConditionEvaluatorSystem<INearbyAccessCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AccessReaderSystem _reader = default!;

    public override float Evaluate(INearbyAccessCondition condition,
        EntityUid entityUid,
        EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transform))
            return 0;

        if (transform.MapUid == null)
            return 0;

        var count = 0f;

        foreach (var (ent, comp) in _lookup.GetEntitiesInRange<AccessReaderComponent>(transform.Coordinates,
                     condition.Range))
        {
            if (!_reader.AreAccessTagsAllowed(condition.Access, comp) ||
                condition.Anchored && !Transform(ent).Anchored)
                continue;
            count++;
        }

        return count / condition.Count;
    }
}
