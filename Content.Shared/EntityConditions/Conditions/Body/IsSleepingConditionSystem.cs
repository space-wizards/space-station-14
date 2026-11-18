using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if the entity is sleeping.
/// </summary>
public sealed partial class IsSleepingConditionSystem : EntityConditionSystem<MobStateComponent, IsSleepingCondition>
{
    [Dependency] private SleepingSystem _sleeping = default!;
    protected override void Condition(Entity<MobStateComponent> entity, ref EntityConditionEvent<IsSleepingCondition> args)
    {
        args.Result = _sleeping.IsSleeping(entity);
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
