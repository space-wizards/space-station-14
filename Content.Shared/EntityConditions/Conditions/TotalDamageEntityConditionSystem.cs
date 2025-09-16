using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TotalDamageEntityConditionSystem : EntityConditionSystem<DamageableComponent, TotalDamage>
{
    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<TotalDamage> args)
    {
        var total = entity.Comp.TotalDamage;
        args.Result = total >= args.Condition.Min && total <= args.Condition.Max;
    }
}

public sealed class TotalDamage : EntityConditionBase<TotalDamage>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;
}
