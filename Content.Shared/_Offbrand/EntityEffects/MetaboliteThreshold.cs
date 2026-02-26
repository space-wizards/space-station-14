using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class MetaboliteThresholdCondition : EntityConditionBase<MetaboliteThresholdCondition>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public bool IncludeBloodstream = true;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var reagentProto = prototype.Index(Reagent);

        if (IncludeBloodstream)
        {
            return Loc.GetString("entity-condition-guidebook-total-dosage-threshold",
                ("reagent", reagentProto.LocalizedName),
                ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
                ("min", Min.Float()));
        }
        else
        {
            return Loc.GetString("entity-condition-guidebook-metabolite-threshold",
                ("reagent", reagentProto.LocalizedName ?? Loc.GetString("entity-condition-guidebook-this-metabolite")),
                ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
                ("min", Min.Float()));
        }
    }
}
