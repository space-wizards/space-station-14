using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed class BrainDamageEntityConditionSystem : EntityConditionSystem<BrainDamageComponent, BrainDamageCondition>
{
    protected override void Condition(Entity<BrainDamageComponent> ent, ref EntityConditionEvent<BrainDamageCondition> args)
    {
        args.Result = ent.Comp.Damage >= args.Condition.Min && ent.Comp.Damage <= args.Condition.Max;
    }
}

public sealed partial class BrainDamageCondition : EntityConditionBase<BrainDamageCondition>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-brain-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
