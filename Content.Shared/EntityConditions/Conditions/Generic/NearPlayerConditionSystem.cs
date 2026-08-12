using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks if the entity is near a player
/// </summary>
public sealed partial class NearPlayerConditionSystem : EntityConditionSystem<TransformComponent, NearPlayerCondition>
{
    [Dependency] private EntityLookupSystem _lookup = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<NearPlayerCondition> args)
    {
        var entities = _lookup.GetEntitiesInRange<MindExaminableComponent>(entity.Comp.Coordinates, args.Condition.Range, LookupFlags.Uncontained);
        var count = 0;

        foreach (var foundEnt in entities)
        {
            if (foundEnt.Owner == entity.Owner)
                continue;

            if (foundEnt.Comp.State != args.Condition.State)
                continue;

            count++;

            if (count >= args.Condition.Count)
            {
                args.Result = true;
                return;
            }
        }
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class NearPlayerCondition : EntityConditionBase<NearPlayerCondition>
{
    /// <summary>
    /// Desired mindstate to check for
    /// </summary>
    [DataField]
    public MindState State = MindState.None;

    /// <summary>
    /// Range around entity to check
    /// </summary>
    [DataField]
    public float Range = 10f;

    /// <summary>
    /// Amount of players that need to be nearby.
    /// </summary>
    [DataField]
    public int Count = 1;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
