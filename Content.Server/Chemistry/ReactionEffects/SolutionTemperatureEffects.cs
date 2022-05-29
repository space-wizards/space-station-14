using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    ///     Sets the temperature of the solution involved with the reaction to a new value.
    /// </summary>
    [DataDefinition]
    public sealed class SetSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The temperature to set the solution to.
        /// </summary>
        [DataField("temperature", required: true)] private float _temperature;

        public override void Effect(ReagentEffectArgs args)
        {
            var solution = args.Source;
            if (solution == null)
                return;

            solution.Temperature = _temperature;
        }
    }

    /// <summary>
    ///     Adjusts the temperature of the solution involved in the reaction.
    /// </summary>
    [DataDefinition]
    [Virtual]
    public class AdjustSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The total change in the thermal energy of the solution.
        /// </summary>
        [DataField("delta", required: true)] protected float Delta;

        /// <summary>
        ///     The minimum temperature this effect can reach.
        /// </summary>
        [DataField("minTemp")] private float _minTemp = 0.0f;

        /// <summary>
        ///     The maximum temperature this effect can reach.
        /// </summary>
        [DataField("maxTemp")] private float _maxTemp = float.PositiveInfinity;

        /// <summary>
        ///     If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
        /// </summary>
        [DataField("scaled")] private bool _scaled;

        /// <summary>
        ///
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        protected virtual float GetDeltaT(Solution solution) => Delta;

        public override void Effect(ReagentEffectArgs args)
        {
            var solution = args.Source;
            if (solution == null)
                return;

            var deltaT = GetDeltaT(solution);
            if (_scaled)
                deltaT = deltaT * (float) args.Quantity;

            if (deltaT == 0.0d)
                return;
            if (deltaT > 0.0d && solution.Temperature >= _maxTemp)
                return;
            if (deltaT < 0.0d && solution.Temperature <= _minTemp)
                return;

            solution.Temperature = MathF.Max(MathF.Min(solution.Temperature + deltaT, _minTemp), _maxTemp);
        }
    }

    /// <summary>
    ///     Adjusts the thermal energy of the solution involved in the reaction.
    /// </summary>
    public sealed class AdjustSolutionThermalEnergyEffect : AdjustSolutionTemperatureEffect
    {
        protected override float GetDeltaT(Solution solution)
        {
            var heatCapacity = solution.HeatCapacity;
            if (heatCapacity == 0.0f)
                return 0.0f;
            return Delta / heatCapacity;
        }
    }
}


