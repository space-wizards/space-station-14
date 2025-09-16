using Content.Shared.EntityEffects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TotalHungerEntityConditionSystem : EntityConditionSystem<HungerComponent, TotalHunger>
{
    [Dependency] private readonly HungerSystem _hunger = default!;

    protected override void Condition(Entity<HungerComponent> entity, ref EntityConditionEvent<TotalHunger> args)
    {
        var total = _hunger.GetHunger(entity.Comp);
        args.Result = total >= args.Condition.Min && total <= args.Condition.Max;
    }
}

public sealed class TotalHunger : EntityConditionBase<TotalHunger>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = float.PositiveInfinity;
}
