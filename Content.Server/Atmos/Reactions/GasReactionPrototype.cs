using System;
using System.Collections.Generic;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

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
    public class GasReactionPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("id")]
        public string ID { get; private set; }

        /// <summary>
        ///     Minimum gas amount requirements.
        /// </summary>
        [YamlField("minimumRequirements")]
        public float[] MinimumRequirements { get; private set; } = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     Minimum temperature requirement.
        /// </summary>
        [YamlField("minimumTemperature")]
        public float MinimumTemperatureRequirement { get; private set; } = Atmospherics.TCMB;

        /// <summary>
        ///     Minimum energy requirement.
        /// </summary>
        [YamlField("minimumEnergy")]
        public float MinimumEnergyRequirement { get; private set; }

        /// <summary>
        ///     Lower numbers are checked/react later than higher numbers.
        ///     If two reactions have the same priority, they may happen in either order.
        /// </summary>
        [YamlField("priority")]
        public int Priority { get; private set; }

        /// <summary>
        ///     A list of effects this will produce.
        /// </summary>
        [YamlField("effects")]
        private List<IGasReactionEffect> _effects;

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
