using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires the solution to be above or below a certain thermal energy.
    ///     Used for things like explosives.
    /// </summary>
    public sealed class SolutionThermalEnergy : ReagentEffectCondition
    {
        [DataField("min")]
        public float Min = 0.0f;

        [DataField("max")]
        public float Max = float.PositiveInfinity;
        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return false;
            if (args.Source.ThermalEnergy < Min)
                return false;
            if (args.Source.ThermalEnergy > Max)
                return false;

            return true;
        }
    }
}
