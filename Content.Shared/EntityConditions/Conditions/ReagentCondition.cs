using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class ReagentCondition : EntityConditionBase<IReagentCondition>, IReagentCondition
{
    /// <inheritdoc/>
    [DataField]
    public FixedPoint2 Min { get; set; } = FixedPoint2.Zero;

    /// <inheritdoc/>
    [DataField]
    public FixedPoint2 Max { get; set; } = FixedPoint2.MaxValue;

    /// <inheritdoc/>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent { get; set; }

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (!prototype.Resolve(Reagent, out var reagentProto))
            return String.Empty;

        return Loc.GetString("entity-condition-guidebook-reagent-threshold",
            ("reagent", reagentProto.LocalizedName),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }

}
