using Content.Server.Temperature.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires the solution entity to be above or below a certain temperature.
    ///     Used for things like cryoxadone and pyroxadone.
    /// </summary>
    public sealed partial class Temperature : ReagentEffectCondition
    {
        [DataField("min")]
        public float Min = 0;

        [DataField("max")]
        public float Max = float.PositiveInfinity;
        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent? temp))
            {
                if (temp.CurrentTemperature > Min && temp.CurrentTemperature < Max)
                    return true;
            }

            return false;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-body-temperature",
                ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
                ("min", Min));
        }
    }
}
