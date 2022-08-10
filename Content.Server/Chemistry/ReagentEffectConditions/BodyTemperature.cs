using Content.Server.Temperature.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires the solution entity to be above or below a certain temperature.
    ///     Used for things like cryoxadone and pyroxadone.
    /// </summary>
    public sealed class Temperature : ReagentEffectCondition
    {
        [DataField("min")]
        public float Min = 0;

        [DataField("max")]
        public float Max = float.MaxValue;
        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent? temp))
            {
                if (temp.CurrentTemperature > Min && temp.CurrentTemperature < Max)
                    return true;
            }

            return false;
        }
    }
}
