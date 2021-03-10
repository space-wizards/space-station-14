#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
    public class GasReactionPrototype : IPrototype
    {
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     Minimum gas amount requirements.
        /// </summary>
        [field: DataField("minimumRequirements")]
        public float[] MinimumRequirements { get; } = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     Maximum temperature requirement.
        /// </summary>
        [field: DataField("maximumTemperature")]
        public float MaximumTemperatureRequirement { get; } = float.MaxValue;

        /// <summary>
        ///     Minimum temperature requirement.
        /// </summary>
        [field: DataField("minimumTemperature")]
        public float MinimumTemperatureRequirement { get; } = Atmospherics.TCMB;

        /// <summary>
        ///     Minimum energy requirement.
        /// </summary>
        [field: DataField("minimumEnergy")]
        public float MinimumEnergyRequirement { get; } = 0f;

        /// <summary>
        ///     Lower numbers are checked/react later than higher numbers.
        ///     If two reactions have the same priority, they may happen in either order.
        /// </summary>
        [field: DataField("priority")]
        public int Priority { get; } = int.MinValue;

        /// <summary>
        ///     A list of effects this will produce.
        /// </summary>
        [DataField("effects")] private List<IGasReactionEffect> _effects = new();

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder holder, GridTileLookupSystem gridLookup)
        {
            var result = ReactionResult.NoReaction;

            foreach (var effect in _effects)
            {
                result |= effect.React(mixture, holder, gridLookup);
            }

            return result;
        }
    }
}
