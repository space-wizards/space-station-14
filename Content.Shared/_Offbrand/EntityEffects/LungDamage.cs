using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class LungDamageCondition : EntityConditionBase<LungDamageCondition>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-lung-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}

public sealed class LungDamageConditionEntitySystem : EntityConditionSystem<LungDamageComponent, LungDamageCondition>
{
    protected override void Condition(Entity<LungDamageComponent> ent, ref EntityConditionEvent<LungDamageCondition> args)
    {
        args.Result = ent.Comp.Damage >= args.Condition.Min && ent.Comp.Damage <= args.Condition.Max;
    }
}
