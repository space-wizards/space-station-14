using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks for a number of entities nearby with the specified accesses.
/// </summary>
public sealed partial class NearbyAccessConditionSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AccessReaderSystem _reader = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<NearbyAccessCondition> args)
    {
        args.Handled = true;

        if (entity.Comp.MapUid == null)
        {
            return;
        }

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

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyAccessCondition : EntityConditionBase<NearbyAccessCondition>, IWithThreshold
{
    // This exists because of door electronics contained inside doors.
    /// <summary>
    /// Does the access entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored = true;

    /// <summary>
    /// Count of entities that need to be nearby.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public List<ProtoId<AccessLevelPrototype>> Access = new();

    [DataField]
    public float Range = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;

    public IWithThreshold.Comparator Comparison => IWithThreshold.Comparator.GreaterEqual;

    public float Threshold => 1;
}
