using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks for a number of entities nearby with the specified accesses.
/// </summary>
public sealed partial class NearbyAccessConditionSystem : EntityConditionSystem<TransformComponent, NearbyAccessCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AccessReaderSystem _reader = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<NearbyAccessCondition> args)
    {
        if (entity.Comp.MapUid == null)
        {
            args.Result = false;
            return;
        }

        var found = false;
        var count = 0;

        foreach (var (ent, comp) in _lookup.GetEntitiesInRange<AccessReaderComponent>(entity.Comp.Coordinates, args.Condition.Range))
        {
            if (!_reader.AreAccessTagsAllowed(args.Condition.Access, comp) ||
                args.Condition.Anchored && !Transform(ent).Anchored)
                continue;

            count++;

            if (count >= args.Condition.Count)
            {
                found = true;
                break;
            }
        }

        args.Result = found;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyAccessCondition : EntityConditionBase<NearbyAccessCondition>
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
}
