using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Reactions
{
    [Flags]
    public enum ReactionResult : byte
    {
        NoReaction = 0,
        Reacting = 1,
        StopReactions = 2,
    }

    public enum GasReaction : byte
    {
        Fire = 0,
    }

    [Prototype("gasReaction")]
    public sealed partial class GasReactionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Minimum gas amount requirements. Reactions that meet these minimum mole requirements
        ///     have their reaction effects run. Generic gas reactions do not have minimum requirements.
        /// </summary>
        [DataField("minimumRequirements")]
        public float[] MinimumRequirements { get; private set; } = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     Maximum temperature requirement.
        /// </summary>
        [DataField("maximumTemperature")]
        public float MaximumTemperatureRequirement { get; private set; } = float.MaxValue;

        /// <summary>
        ///     Minimum temperature requirement.
        /// </summary>
        [DataField("minimumTemperature")]
        public float MinimumTemperatureRequirement { get; private set; } = Atmospherics.TCMB;

        /// <summary>
        /// If this is a generic gas reaction, multiply the initial rate by this. The default is reasonable for
        /// synthesis reactions. Consider raising this for fires.
        /// </summary>
        [DataField("rateMultiplier")]
        public float RateMultiplier = 1f;

        /// <summary>
        ///     Minimum energy requirement.
        /// </summary>
        [DataField("minimumEnergy")]
        public float MinimumEnergyRequirement { get; private set; } = 0f;

        /// <summary>
        ///     Lower numbers are checked/react later than higher numbers.
        ///     If two reactions have the same priority, they may happen in either order.
        /// </summary>
        [DataField("priority")]
        public int Priority { get; private set; } = int.MinValue;

        /// <summary>
        ///     A list of effects this will produce.
        /// </summary>
        [DataField("effects")] private List<IGasReactionEffect> _effects = new();

        /// <summary>
        ///     Energy released by the reaction.
        /// </summary>
        [DataField("enthalpy")]
        public float Enthalpy;

        /// <summary>
        /// Integer gas IDs and integer ratios required in the reaction. If this is defined, the
        /// generic gas reaction will run.
        /// </summary>
        [DataField("reactants")]
        public Dictionary<Gas, int> Reactants = new();

        /// <summary>
        /// Integer gas IDs and integer ratios of reaction products.
        /// </summary>
        [DataField("products")]
        public Dictionary<Gas, int> Products = new();

        /// <summary>
        /// Integer gas IDs and how much they modify the activation energy (J/mol).
        /// </summary>
        [DataField("catalysts")]
        public Dictionary<Gas, int> Catalysts = new();

        /// <summary>
        /// Process all reaction effects.
        /// </summary>
        /// <param name="mixture">The gas mixture to react</param>
        /// <param name="holder">The container of this gas mixture</param>
        /// <param name="atmosphereSystem">The atmosphere system</param>
        /// <param name="heatScale">Scaling factor that should be applied to all heat input or outputs.</param>
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            var result = ReactionResult.NoReaction;

            foreach (var effect in _effects)
            {
                result |= effect.React(mixture, holder, atmosphereSystem, heatScale);
            }

            return result;
        }
    }
}
