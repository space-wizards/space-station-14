using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class ReagentThresholdEntityConditionSystem : EntityConditionSystem<SolutionComponent, ReagentThreshold>
{
    protected override void Condition(Entity<SolutionComponent> entity, ref EntityConditionEvent<ReagentThreshold> args)
    {
        var quant = entity.Comp.Solution.GetTotalPrototypeQuantity(args.Condition.Reagent);

        args.Result = quant >= args.Condition.Min && quant <= args.Condition.Max;
    }
}

public sealed partial class ReagentThreshold : EntityConditionBase<ReagentThreshold>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (!prototype.Resolve(Reagent, out var reagentProto))
            return String.Empty;

        return Loc.GetString("reagent-effect-condition-guidebook-reagent-threshold",
            ("reagent", reagentProto.LocalizedName ?? Loc.GetString("reagent-effect-condition-guidebook-this-reagent")),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
