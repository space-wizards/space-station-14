using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class ReagentEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<SolutionComponent> entity, ref ConditionEvaluationEvent<ReagentCondition> args)
    {
        var soln = entity.Comp.Solution;

        var quant = soln.GetTotalPrototypeQuantity(args.Condition.Reagent);

        args.Value = ((quant - args.Condition.Min) / (args.Condition.Max - args.Condition.Min)).Float();

        args.Handled = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class ReagentCondition : EntityConditionBase<ReagentCondition>, IWithBoundary
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

        return Loc.GetString("entity-condition-guidebook-reagent-threshold",
            ("reagent", reagentProto.LocalizedName),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }

    float IWithBoundary.LowerBound => 0;

    bool IWithBoundary.IncludeLowerBound => true;

    float IWithBoundary.UpperBound => 1;

    bool IWithBoundary.IncludeUpperBound => true;

}
