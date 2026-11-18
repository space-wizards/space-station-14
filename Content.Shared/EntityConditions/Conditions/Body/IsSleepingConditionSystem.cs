using Content.Shared.Bed.Sleep;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if the entity is sleeping.
/// </summary>
public sealed partial class IsSleepingConditionSystem : EntityConditionSystem<TransformComponent, IsSleepingCondition>
{
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<IsSleepingCondition> args)
    {
        args.Result = HasComp<SleepingComponent>(entity);
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class IsSleepingCondition : EntityConditionBase<IsSleepingCondition>
{
    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-is-sleeping-condition");
    }
};
