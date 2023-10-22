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
    public sealed class GasReactionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Minimum gas amount requirements.
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

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
        {
            var result = ReactionResult.NoReaction;

            foreach (var effect in _effects)
            {
                result |= effect.React(mixture, holder, atmosphereSystem);
            }

            return result;
        }
    }
}
