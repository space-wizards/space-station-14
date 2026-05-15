using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if this entity's hunger is within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class TotalHungerEntityConditionSystem : EntityConditionSystem<HungerComponent, HungerCondition>
{
    [Dependency] private readonly HungerSystem _hunger = default!;

    protected override void Condition(Entity<HungerComponent> entity, ref EntityConditionEvent<HungerCondition> args)
    {
        var total = _hunger.GetHunger(entity.Comp);
        args.Result = total >= args.Condition.Min && total <= args.Condition.Max;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class HungerCondition : EntityConditionBase<HungerCondition>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = float.PositiveInfinity;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-total-hunger", ("max", float.IsPositiveInfinity(Max) ? int.MaxValue : Max), ("min", Min));
}
