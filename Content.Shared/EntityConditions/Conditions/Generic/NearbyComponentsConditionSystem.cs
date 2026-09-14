using System.Numerics;
using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks if an entity is in range of a specified number of entities with specific components.
/// </summary>
public sealed partial class NearbyComponentsConditionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    [SubscribeLocalEvent]
   private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent args)
   {
       if (args.Handled || args.Condition is not NearbyComponentsCondition condition)
           return;
       args.Handled = true;

        var worldPos = _transform.GetWorldPosition(entity.Comp);
        var count = 0.0f;

        var box = Box2.CenteredAround(worldPos, new Vector2(condition.Range));

        foreach (var ent in _lookup.GetEntitiesIntersecting(entity.Comp.MapID, box))
        {
            if (condition.Anchored && !Transform(ent).Anchored)
                continue;

            foreach (var compType in condition.Components.Values)
            {
                if (!HasComp(ent, compType.Component.GetType()))
                    continue;
                count++;
            }
        }

        args.Value = count / condition.Count;
   }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearbyComponentsCondition : EntityConditionBase<NearbyComponentsCondition>, IWithThreshold
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

    public IWithThreshold.Comparator Comparison => IWithThreshold.Comparator.GreaterEqual;

    public float Threshold => 1;
}
