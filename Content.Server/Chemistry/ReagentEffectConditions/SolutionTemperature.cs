using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires the solution to be above or below a certain temperature.
    ///     Used for things like explosives.
    /// </summary>
    public class SolutionTemperature : ReagentEffectCondition
    {
        [DataField("min")]
        public double Min = 0;

        [DataField("max")]
        public double Max = double.PositiveInfinity;
        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return false;
            if (args.Source.Temperature < Min)
                return false;
            if (args.Source.Temperature > Max)
                return false;

            return true;
        }
    }
}
