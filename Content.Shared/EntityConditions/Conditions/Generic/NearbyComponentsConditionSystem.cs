using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks if an entity is in range of a specified number of entities with specific components.
/// </summary>
public sealed partial class NearbyComponentsConditionSystem : EntityConditionSystem<TransformComponent, NearbyComponentsCondition>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<NearbyComponentsCondition> args)
    {
        var worldPos = _transform.GetWorldPosition(entity.Comp);
        var count = 0;

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

                if (count >= args.Condition.Count)
                {
                    args.Result = true;
                    return;
                }
            }
        }

        args.Result = false;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyComponentsCondition : EntityConditionBase<NearbyComponentsCondition>
{
    /// <summary>
    /// Does the entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored;

    [DataField]
    public int Count;

    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField]
    public float Range = 10f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
