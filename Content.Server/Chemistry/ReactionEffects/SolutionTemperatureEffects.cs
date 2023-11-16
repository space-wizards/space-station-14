using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using static Robust.Shared.Physics.DynamicTree;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    ///     Sets the temperature of the solution involved with the reaction to a new value.
    /// </summary>
    [DataDefinition]
    public sealed partial class SetSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The temperature to set the solution to.
        /// </summary>
        [DataField("temperature", required: true)] private float _temperature;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-set-solution-temperature-effect",
                ("chance", Probability), ("temperature", _temperature));

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
    public sealed partial class AdjustSolutionTemperatureEffect : ReagentEffect
    {
        /// <summary>
        ///     The change in temperature.
        /// </summary>
        [DataField("delta", required: true)] private float _delta;

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

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-adjust-solution-temperature-effect",
                ("chance", Probability), ("deltasign", MathF.Sign(_delta)), ("mintemp", _minTemp), ("maxtemp", _maxTemp));

        public override void Effect(ReagentEffectArgs args)
        {
            var solution = args.Source;
            if (solution == null || solution.Volume == 0)
                return;

            var deltaT = _scaled ? _delta * (float) args.Quantity : _delta;
            solution.Temperature = Math.Clamp(solution.Temperature + deltaT, _minTemp, _maxTemp);
        }
    }

    /// <summary>
    ///     Adjusts the thermal energy of the solution involved in the reaction.
    /// </summary>
    public sealed partial class AdjustSolutionThermalEnergyEffect : ReagentEffect
    {
        /// <summary>
        ///     The change in energy.
        /// </summary>
        [DataField("delta", required: true)] private float _delta;

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

        public override void Effect(ReagentEffectArgs args)
        {
            var solution = args.Source;
            if (solution == null || solution.Volume == 0)
                return;

            if (_delta > 0 && solution.Temperature >= _maxTemp)
                return;
            if (_delta < 0 && solution.Temperature <= _minTemp)
                return;

            var heatCap = solution.GetHeatCapacity(null);
            var deltaT = _scaled
                ? _delta / heatCap * (float) args.Quantity
                : _delta / heatCap;

            solution.Temperature = Math.Clamp(solution.Temperature + deltaT, _minTemp, _maxTemp);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-adjust-solution-temperature-effect",
                ("chance", Probability), ("deltasign", MathF.Sign(_delta)), ("mintemp", _minTemp), ("maxtemp", _maxTemp));
    }

}


