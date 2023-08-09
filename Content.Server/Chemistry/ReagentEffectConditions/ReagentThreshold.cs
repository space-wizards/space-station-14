using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Used for implementing reagent effects that require a certain amount of reagent before it should be applied.
    ///     For instance, overdoses.
    ///
    ///     This can also trigger on -other- reagents, not just the one metabolizing. By default, it uses the
    ///     one being metabolized.
    /// </summary>
    public sealed class ReagentThreshold : ReagentEffectCondition
    {
        [DataField("min")]
        public FixedPoint2 Min = FixedPoint2.Zero;

        [DataField("max")]
        public FixedPoint2 Max = FixedPoint2.MaxValue;

        [DataField("reagent")]
        public string? Reagent;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (Reagent == null)
                Reagent = args.Reagent.ID;

            var quant = FixedPoint2.Zero;
            if (args.Source != null && args.Source.ContainsReagent(Reagent))
            {
                quant = args.Source.GetReagentQuantity(args.Reagent.ID);
            }

            return quant >= Min && quant <= Max;
        }
    }
}
