using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Used for implementing reagent effects that require a certain amount of reagent before it should be applied.
    ///     For instance, overdoses.
    ///
    ///     This can also trigger on -other- reagents, not just the one metabolizing. By default, it uses the
    ///     one being metabolized.
    /// </summary>
    public sealed partial class ReagentThreshold : ReagentEffectCondition
    {
        [DataField("min")]
        public FixedPoint2 Min = FixedPoint2.Zero;

        [DataField("max")]
        public FixedPoint2 Max = FixedPoint2.MaxValue;

        // TODO use ReagentId
        [DataField("reagent")]
        public string? Reagent;

        public override bool Condition(ReagentEffectArgs args)
        {
            var reagent = Reagent ?? args.Reagent?.ID;
            if (reagent == null)
                return true; // No condition to apply.

            var quant = FixedPoint2.Zero;
            if (args.Source != null)
                quant = args.Source.GetTotalPrototypeQuantity(reagent);

            return quant >= Min && quant <= Max;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            ReagentPrototype? reagentProto = null;
            if (Reagent is not null)
                prototype.TryIndex(Reagent, out reagentProto);

            return Loc.GetString("reagent-effect-condition-guidebook-reagent-threshold",
                ("reagent", reagentProto?.LocalizedName ?? "this reagent"),
                ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
                ("min", Min.Float()));
        }
    }
}
