using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();
        private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;

        /// <summary>
        ///     Cached array of gas specific heats.
        /// </summary>
        public float[] GasSpecificHeats => _gasSpecificHeats;

        public string?[] GasReagents = new string[Atmospherics.TotalNumberOfGases];

        private void InitializeGases()
        {
            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

            for (var i = 0; i < GasPrototypes.Length; i++)
            {
                _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat;
                GasReagents[i] = GasPrototypes[i].Reagent;
            }
        }

        /// <summary>
        ///     Calculates the heat capacity for a gas mixture.
        /// </summary>
        public float GetHeatCapacity(GasMixture mixture)
        {
            return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetHeatCapacityCalculation(float[] moles, bool immutable)
        {
            // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
            if (immutable && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            Span<float> tmp = stackalloc float[moles.Length];
            NumericsHelpers.Multiply(moles, GasSpecificHeats, tmp);
            return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
        }

        /// <summary>
        ///     Calculates the thermal energy for a gas mixture.
        /// </summary>
        public float GetThermalEnergy(GasMixture mixture)
        {
            return mixture.Temperature * GetHeatCapacity(mixture);
        }

        /// <summary>
        ///     Calculates the thermal energy for a gas mixture, using a cached heat capacity value.
        /// </summary>
        public float GetThermalEnergy(GasMixture mixture, float cachedHeatCapacity)
        {
            return mixture.Temperature * cachedHeatCapacity;
        }

        /// <summary>
        ///     Merges the <see cref="giver"/> gas mixture into the <see cref="receiver"/> gas mixture.
        ///     The <see cref="giver"/> gas mixture is not modified by this method.
        /// </summary>
        public void Merge(GasMixture receiver, GasMixture giver)
        {
            if (receiver.Immutable) return;

            if (MathF.Abs(receiver.Temperature - giver.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var receiverHeatCapacity = GetHeatCapacity(receiver);
                var giverHeatCapacity = GetHeatCapacity(giver);
                var combinedHeatCapacity = receiverHeatCapacity + giverHeatCapacity;
                if (combinedHeatCapacity > 0f)
                {
                    receiver.Temperature = (GetThermalEnergy(giver, giverHeatCapacity) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                }
            }

            NumericsHelpers.Add(receiver.Moles, giver.Moles);
        }

        /// <summary>
        ///     Releases gas from this mixture to the output mixture.
        ///     If the output mixture is null, then this is being released into space.
        ///     It can't transfer air to a mixture with higher pressure.
        /// </summary>
        public bool ReleaseGasTo(GasMixture mixture, GasMixture? output, float targetPressure)
        {
            var outputStartingPressure = output?.Pressure ?? 0;
            var inputStartingPressure = mixture.Pressure;

            if (outputStartingPressure >= MathF.Min(targetPressure, inputStartingPressure - 10))
                // No need to pump gas if the target is already reached or input pressure is too low.
                // Need at least 10 kPa difference to overcome friction in the mechanism.
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var pressureDelta = MathF.Min(targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure) / 2f);
            var transferMoles = pressureDelta * (output?.Volume ?? Atmospherics.CellVolume) / (mixture.Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = mixture.Remove(transferMoles);

            if(output != null)
                Merge(output, removed);

            return true;
        }

        /// <summary>
        ///     Pump gas from this mixture to the output mixture.
        ///     Amount depends on target pressure.
        /// </summary>
        /// <param name="mixture">The mixture to pump the gas from</param>
        /// <param name="output">The mixture to pump the gas to</param>
        /// <param name="targetPressure">The target pressure to reach</param>
        /// <returns>Whether we could pump air to the output or not</returns>
        public bool PumpGasTo(GasMixture mixture, GasMixture output, float targetPressure)
        {
            var outputStartingPressure = output.Pressure;
            var pressureDelta = targetPressure - outputStartingPressure;

            if (pressureDelta < 0.01)
                // No need to pump gas, we've reached the target.
                return false;

            if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0)) return false;

            // We calculate the necessary moles to transfer with the ideal gas law.
            var transferMoles = pressureDelta * output.Volume / (mixture.Temperature * Atmospherics.R);

            // And now we transfer the gas.
            var removed = mixture.Remove(transferMoles);
            Merge(output, removed);
            return true;
        }

        /// <summary>
        ///     Scrubs specified gases from a gas mixture into a <see cref="destination"/> gas mixture.
        /// </summary>
        public void ScrubInto(GasMixture mixture, GasMixture destination, IReadOnlyCollection<Gas> filterGases)
        {
            var buffer = new GasMixture(mixture.Volume){Temperature = mixture.Temperature};

            foreach (var gas in filterGases)
            {
                buffer.AdjustMoles(gas, mixture.GetMoles(gas));
                mixture.SetMoles(gas, 0f);
            }

            Merge(destination, buffer);
        }

        /// <summary>
        ///     Checks whether a gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="air">Mixture to be checked.</param>
        /// <returns>Whether the mixture is probably safe.</returns>
        public bool IsMixtureProbablySafe(GasMixture? air)
        {
            // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
            if (air == null)
                return false;

            switch (air.Pressure)
            {
                case <= Atmospherics.WarningLowPressure:
                case >= Atmospherics.WarningHighPressure:
                    return false;
            }

            switch (air.Temperature)
            {
                case <= 260:
                case >= 360:
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Performs reactions for a given gas mixture on an optional holder.
        /// </summary>
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        {
            var reaction = ReactionResult.NoReaction;
            var temperature = mixture.Temperature;
            var energy = GetThermalEnergy(mixture);

            foreach (var prototype in GasReactions)
            {
                if (energy < prototype.MinimumEnergyRequirement ||
                    temperature < prototype.MinimumTemperatureRequirement ||
                    temperature > prototype.MaximumTemperatureRequirement)
                    continue;

                var doReaction = true;
                for (var i = 0; i < prototype.MinimumRequirements.Length; i++)
                {
                    if(i >= Atmospherics.TotalNumberOfGases)
                        throw new IndexOutOfRangeException("Reaction Gas Minimum Requirements Array Prototype exceeds total number of gases!");

                    var req = prototype.MinimumRequirements[i];

                    if (!(mixture.GetMoles(i) < req)) continue;
                    doReaction = false;
                    break;
                }

                if (!doReaction)
                    continue;

                reaction = prototype.React(mixture, holder, this);
                if(reaction.HasFlag(ReactionResult.StopReactions))
                    break;
            }

            return reaction;
        }
    }
}
