using System;
using System.Collections.Generic;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
        public string ID { get; private set; }

        /// <summary>
        ///     Minimum gas amount requirements.
        /// </summary>
        public float[] MinimumRequirements { get; private set; }

        /// <summary>
        ///     Minimum temperature requirement.
        /// </summary>
        public float MinimumTemperatureRequirement { get; private set; }

        /// <summary>
        ///     Minimum energy requirement.
        /// </summary>
        public float MinimumEnergyRequirement { get; private set; }

        /// <summary>
        ///     Lower numbers are checked/react later than higher numbers.
        ///     If two reactions have the same priority, they may happen in either order.
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        ///     A list of effects this will produce.
        /// </summary>
        private List<IGasReactionEffect> _effects;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Priority, "priority", 100);
            serializer.DataField(this, x => x.MinimumRequirements, "minimumRequirements", new float[Atmospherics.TotalNumberOfGases]);
            serializer.DataField(this, x => x.MinimumTemperatureRequirement, "minimumTemperature", Atmospherics.TCMB);
            serializer.DataField(this, x => x.MinimumEnergyRequirement, "minimumEnergy", 0f);
            serializer.DataField(ref _effects, "effects", new List<IGasReactionEffect>());
        }

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
