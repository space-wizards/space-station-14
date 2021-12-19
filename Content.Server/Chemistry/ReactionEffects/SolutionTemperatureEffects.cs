using System;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    ///     Sets the temperature of the solution involved with the reaction to a new value.
    /// </summary>
    [DataDefinition]
    public class SetSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The temperature to set the solution to.
        /// </summary>
        [DataField("temperature")] private double _temperature;

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
    public class AdjustSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The total change in the thermal energy of the solution.
        /// </summary>
        [DataField("delta")] protected double Delta;

        /// <summary>
        ///     The minimum temperature this effect can reach.
        /// </summary>
        [DataField("minTemp")] private double _minTemp = 0.0d;

        /// <summary>
        ///     The maximum temperature this effect can reach.
        /// </summary>
        [DataField("maxTemp")] private double _maxTemp = double.PositiveInfinity;

        /// <summary>
        ///     If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
        /// </summary>
        [DataField("scaled")] private bool _scaled;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        protected virtual double GetDeltaT(Solution solution) => Delta;

        public override void Effect(ReagentEffectArgs args)
        {
            var solution = args.Source;
            if (solution == null)
                return;

            var deltaT = GetDeltaT(solution);
            if (_scaled)
                deltaT = deltaT * (double)args.Quantity;

            if (deltaT == 0.0d)
                return;
            if (deltaT > 0.0d && solution.Temperature >= _maxTemp)
                return;
            if (deltaT < 0.0d && solution.Temperature <= _minTemp)
                return;

            solution.Temperature = MathF.Max(MathF.Min((float) (solution.Temperature + deltaT), (float) _minTemp), (float) _maxTemp);
        }
    }

    /// <summary>
    ///     Adjusts the thermal energy of the solution involved in the reaction.
    /// </summary>
    public class AdjustSolutionThermalEnergyEffect : AdjustSolutionTemperatureEffect
    {
        protected override double GetDeltaT(Solution solution)
        {
            var heatCapacity = solution.HeatCapacity;
            if (heatCapacity == 0.0d)
                return 0.0d;
            return Delta / heatCapacity;
        }
    }
}


