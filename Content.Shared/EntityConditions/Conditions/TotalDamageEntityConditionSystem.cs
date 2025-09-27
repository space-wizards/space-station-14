using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TotalDamageEntityConditionSystem : EntityConditionSystem<DamageableComponent, TotalDamage>
{
    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<TotalDamage> args)
    {
        var total = entity.Comp.TotalDamage;
        args.Result = total >= args.Condition.Min && total <= args.Condition.Max;
    }
}

public sealed partial class TotalDamage : EntityConditionBase<TotalDamage>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("reagent-effect-condition-guidebook-total-damage",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
}
