using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();
        private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;
        public float[] GasSpecificHeats => _gasSpecificHeats;

        private void InitializeGases()
        {
            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

            for (var i = 0; i < GasPrototypes.Length; i++)
            {
                _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat;
            }
        }

        public float GetHeatCapacity(GasMixture mixture)
        {
            return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
        }

        public float GetHeatCapacityArchived(GasMixture mixture)
        {
            return GetHeatCapacityCalculation(mixture.MolesArchived, mixture.Immutable);
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

        public float GetThermalEnergy(GasMixture mixture)
        {
            return mixture.Temperature * GetHeatCapacity(mixture);
        }

        public float GetThermalEnergy(GasMixture mixture, float cachedHeatCapacity)
        {
            return mixture.Temperature * cachedHeatCapacity;
        }

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

        public float Share(GasMixture receiver, GasMixture sharer, int atmosAdjacentTurfs)
        {
            var temperatureDelta = receiver.TemperatureArchived - sharer.TemperatureArchived;
            var absTemperatureDelta = Math.Abs(temperatureDelta);
            var oldHeatCapacity = 0f;
            var oldSharerHeatCapacity = 0f;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                oldHeatCapacity = GetHeatCapacity(receiver);
                oldSharerHeatCapacity = GetHeatCapacity(sharer);
            }

            var heatCapacityToSharer = 0f;
            var heatCapacitySharerToThis = 0f;
            var movedMoles = 0f;
            var absMovedMoles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var thisValue = receiver.Moles[i];
                var sharerValue = sharer.Moles[i];
                var delta = (thisValue - sharerValue) / (atmosAdjacentTurfs + 1);
                if (!(MathF.Abs(delta) >= Atmospherics.GasMinMoles)) continue;
                if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
                {
                    var gasHeatCapacity = delta * GasSpecificHeats[i];
                    if (delta > 0)
                    {
                        heatCapacityToSharer += gasHeatCapacity;
                    }
                    else
                    {
                        heatCapacitySharerToThis -= gasHeatCapacity;
                    }
                }

                if (!receiver.Immutable) receiver.Moles[i] -= delta;
                if (!sharer.Immutable) sharer.Moles[i] += delta;
                movedMoles += delta;
                absMovedMoles += MathF.Abs(delta);
            }

            receiver.LastShare = absMovedMoles;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var newHeatCapacity = oldHeatCapacity + heatCapacitySharerToThis - heatCapacityToSharer;
                var newSharerHeatCapacity = oldSharerHeatCapacity + heatCapacityToSharer - heatCapacitySharerToThis;

                // Transfer of thermal energy (via changed heat capacity) between self and sharer.
                if (!receiver.Immutable && newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    receiver.Temperature = ((oldHeatCapacity * receiver.Temperature) - (heatCapacityToSharer * receiver.TemperatureArchived) + (heatCapacitySharerToThis * sharer.TemperatureArchived)) / newHeatCapacity;
                }

                if (!sharer.Immutable && newSharerHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    sharer.Temperature = ((oldSharerHeatCapacity * sharer.Temperature) - (heatCapacitySharerToThis * sharer.TemperatureArchived) + (heatCapacityToSharer*receiver.TemperatureArchived)) / newSharerHeatCapacity;
                }

                // Thermal energy of the system (self and sharer) is unchanged.

                if (MathF.Abs(oldSharerHeatCapacity) > Atmospherics.MinimumHeatCapacity)
                {
                    if (MathF.Abs(newSharerHeatCapacity / oldSharerHeatCapacity - 1) < 0.1)
                    {
                        TemperatureShare(receiver, sharer, Atmospherics.OpenHeatTransferCoefficient);
                    }
                }
            }

            if (!(temperatureDelta > Atmospherics.MinimumTemperatureToMove) &&
                !(MathF.Abs(movedMoles) > Atmospherics.MinimumMolesDeltaToMove)) return 0f;
            var moles = receiver.TotalMoles;
            var theirMoles = sharer.TotalMoles;

            return (receiver.TemperatureArchived * (moles + movedMoles)) - (sharer.TemperatureArchived * (theirMoles - movedMoles)) * Atmospherics.R / receiver.Volume;

        }

        public float TemperatureShare(GasMixture receiver, GasMixture sharer, float conductionCoefficient)
        {
            var temperatureDelta = receiver.TemperatureArchived - sharer.TemperatureArchived;
            if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var heatCapacity = GetHeatCapacityArchived(receiver);
                var sharerHeatCapacity = GetHeatCapacityArchived(sharer);

                if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity && heatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    var heat = conductionCoefficient * temperatureDelta * (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                    if (!receiver.Immutable)
                        receiver.Temperature = MathF.Abs(MathF.Max(receiver.Temperature - heat / heatCapacity, Atmospherics.TCMB));

                    if (!sharer.Immutable)
                        sharer.Temperature = MathF.Abs(MathF.Max(sharer.Temperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
                }
            }

            return sharer.Temperature;
        }

        public float TemperatureShare(GasMixture receiver, float conductionCoefficient, float sharerTemperature, float sharerHeatCapacity)
        {
            var temperatureDelta = receiver.TemperatureArchived - sharerTemperature;
            if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var heatCapacity = GetHeatCapacityArchived(receiver);

                if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity && heatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    var heat = conductionCoefficient * temperatureDelta * (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                    if (!receiver.Immutable)
                        receiver.Temperature = MathF.Abs(MathF.Max(receiver.Temperature - heat / heatCapacity, Atmospherics.TCMB));

                    sharerTemperature = MathF.Abs(MathF.Max(sharerTemperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
                }
            }

            return sharerTemperature;
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
                    if(i > Atmospherics.TotalNumberOfGases)
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
