using Content.Shared.Conditions;
using Content.Shared.Conditions.Interfaces;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks for entities matching the whitelist in range.
/// </summary>
public sealed partial class NearbyEntitiesConditionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<NearbyEntitiesCondition> args)
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


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyEntitiesCondition : EntityConditionBase<NearbyEntitiesCondition>, IWithThreshold
{
    /// <summary>
    /// How many of the entity need to be nearby.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public float Range = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;

    public IWithThreshold.Comparator Comparison => IWithThreshold.Comparator.GreaterEqual;

    public float Threshold => 1;
}
