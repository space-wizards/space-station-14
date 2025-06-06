using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
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
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions;

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
                _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat / HeatScale;
                GasReagents[i] = GasPrototypes[i].Reagent;
            }
        }

        /// <summary>
        ///     Calculates the heat capacity for a gas mixture.
        /// </summary>
        /// <param name="mixture">The mixture whose heat capacity should be calculated</param>
        /// <param name="applyScaling"> Whether the internal heat capacity scaling should be applied. This should not be
        /// used outside of atmospheric related heat transfer.</param>
        /// <returns></returns>
        public float GetHeatCapacity(GasMixture mixture, bool applyScaling)
        {
            var scale = GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);

            // By default GetHeatCapacityCalculation() has the heat-scale divisor pre-applied.
            // So if we want the un-scaled heat capacity, we have to multiply by the scale.
            return applyScaling ? scale : scale * HeatScale;
        }

        private float GetHeatCapacity(GasMixture mixture)
            =>  GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetHeatCapacityCalculation(float[] moles, bool space)
        {
            // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
            if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            Span<float> tmp = stackalloc float[moles.Length];
            NumericsHelpers.Multiply(moles, GasSpecificHeats, tmp);
            // Adjust heat capacity by speedup, because this is primarily what
            // determines how quickly gases heat up/cool.
            return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
        }

        /// <summary>
        ///     Return speedup factor for pumped or flow-based devices that depend on MaxTransferRate.
        /// </summary>
        public float PumpSpeedup()
        {
            return Speedup;
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
        ///     Add 'dQ' Joules of energy into 'mixture'.
        /// </summary>
        public void AddHeat(GasMixture mixture, float dQ)
        {
            var c = GetHeatCapacity(mixture);
            float dT = dQ / c;
            mixture.Temperature += dT;
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
                if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    receiver.Temperature = (GetThermalEnergy(giver, giverHeatCapacity) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                }
            }

            NumericsHelpers.Add(receiver.Moles, giver.Moles);
        }

        /// <summary>
        ///     Divides a source gas mixture into several recipient mixtures, scaled by their relative volumes. Does not
        ///     modify the source gas mixture. Used for pipe network splitting. Note that the total destination volume
        ///     may be larger or smaller than the source mixture.
        /// </summary>
        public void DivideInto(GasMixture source, List<GasMixture> receivers)
        {
            var totalVolume = 0f;
            foreach (var receiver in receivers)
            {
                if (!receiver.Immutable)
                    totalVolume += receiver.Volume;
            }

            float? sourceHeatCapacity = null;
            var buffer = new float[Atmospherics.AdjustedNumberOfGases];

            foreach (var receiver in receivers)
            {
                if (receiver.Immutable)
                    continue;

                var fraction = receiver.Volume / totalVolume;

                // Set temperature, if necessary.
                if (MathF.Abs(receiver.Temperature - source.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
                {
                    // Often this divides a pipe net into new and completely empty pipe nets
                    if (receiver.TotalMoles == 0)
                        receiver.Temperature = source.Temperature;
                    else
                    {
                        sourceHeatCapacity ??= GetHeatCapacity(source);
                        var receiverHeatCapacity = GetHeatCapacity(receiver);
                        var combinedHeatCapacity = receiverHeatCapacity + sourceHeatCapacity.Value * fraction;
                        if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                            receiver.Temperature = (GetThermalEnergy(source, sourceHeatCapacity.Value * fraction) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                    }
                }

                // transfer moles
                NumericsHelpers.Multiply(source.Moles, fraction, buffer);
                NumericsHelpers.Add(receiver.Moles, buffer);
            }
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
        /// Calculates the dimensionless fraction of gas required to equalize pressure between two gas mixtures.
        /// </summary>
        /// <param name="gasMixture1">The first gas mixture involved in the pressure equalization.
        /// This mixture should be the one you always expect to be the highest pressure.</param>
        /// <param name="gasMixture2">The second gas mixture involved in the pressure equalization.</param>
        /// <returns>A float (from 0 to 1) representing the dimensionless fraction of gas that needs to be transferred from the
        /// mixture of higher pressure to the mixture of lower pressure.</returns>
        /// <remarks>
        /// <para>
        /// This properly takes into account the effect
        /// of gas merging from inlet to outlet affecting the temperature
        /// (and possibly increasing the pressure) in the outlet.
        /// </para>
        /// <para>
        /// The gas is assumed to expand freely,
        /// so the temperature of the gas with the greater pressure is not changing.
        /// </para>
        /// </remarks>
        /// <example>
        /// If you want to calculate the moles required to equalize pressure between an inlet and an outlet,
        /// multiply the fraction returned by the source moles.
        /// </example>
        public float FractionToEqualizePressure(GasMixture gasMixture1, GasMixture gasMixture2)
        {
            /*
            Problem: the gas being merged from the inlet to the outlet could affect the
            temp. of the gas and cause a pressure rise.
            We want the pressure to be equalized, so we have to account for this.

            For clarity, let's assume that gasMixture1 is the inlet and gasMixture2 is the outlet.

            We require mechanical equilibrium, so \( P_1' = P_2' \)

            Before the transfer, we have:
            \( P_1 = \frac{n_1 R T_1}{V_1} \)
            \( P_2 = \frac{n_2 R T_2}{V_2} \)

            After removing fraction \( x \) moles from the inlet, we have:
            \( P_1' = \frac{(1 - x) n_1 R T_1}{V_1} \)

            The outlet will gain the same \( x n_1 \) moles of gas.
            So \( n_2' = n_2 + x n_1 \)

            After mixing, the outlet temperature will be changed.
            Denote the new mixture temperature as \( T_2' \).
            Volume is constant.
            So we have:
            \( P_2' = \frac{(n_2 + x n_1) R T_2}{V_2} \)

            The total energy of the incoming inlet to outlet gas at \( T_1 \) plus the existing energy of the outlet gas at \( T_2 \)
            will be equal to the energy of the new outlet gas at \( T_2' \).
            This leads to the following derivation:
            \( x n_1 C_1 T_1 + n_2 C_2 T_2 = (x n_1 C_1 + n_2 C_2) T_2' \)

            Where \( C_1 \) and \( C_2 \) are the heat capacities of the inlet and outlet gases, respectively.

            Solving for \( T_2' \) gives us:
            \( T_2' = \frac{x n_1 C_1 T_1 + n_2 C_2 T_2}{x n_1 C_1 + n_2 C_2} \)

            Once again, we require mechanical equilibrium (\( P_1' = P_2' \)),
            so we can substitute \( T_2' \) into the pressure equation:

            \( \frac{(1 - x) n_1 R T_1}{V_1} =
            \frac{(n_2 + x n_1) R}{V_2} \cdot
            \frac{x n_1 C_1 T_1 + n_2 C_2 T_2}
            {x n_1 C_1 + n_2 C_2} \)

            Now it's a matter of solving for \( x \).
            Not going to show the full derivation here, just steps.
            1. Cancel common factor \( R \).
            2. Multiply both sides by \( x n_1 C_1 + n_2 C_2 \), so that everything
            becomes a polynomial in terms of \( x \).
            3. Expand both sides.
            4. Collect like powers of \( x \).
            5. After collecting, you should end up with a polynomial of the form:

            \( (-n_1 C_1 T_1 (1 + \frac{V_2}{V_1})) x^2 +
            (n_1 T_1 \frac{V_2}{V_1} (C_1 - C_2) - n_2 C_1 T_1 - n_1 C_2 T_2) x +
            (n_1 T_1 \frac{V_2}{V_1} C_2 - n_2 C_2 T_2) = 0 \)

            Divide through by \( n_1 C_1 T_1 \) and replace each ratio with a symbol for clarity:
            \( k_V = \frac{V_2}{V_1} \)
            \( k_n = \frac{n_2}{n_1} \)
            \( k_T = \frac{T_2}{T_1} \)
            \( k_C = \frac{C_2}{C_1} \)
            */

            // Ensure that P_1 > P_2 so the quadratic works out.
            if (gasMixture1.Pressure < gasMixture2.Pressure)
            {
                (gasMixture1, gasMixture2) = (gasMixture2, gasMixture1);
            }

            // Establish the dimensionless ratios.
            var volumeRatio = gasMixture2.Volume / gasMixture1.Volume;
            var molesRatio = gasMixture2.TotalMoles / gasMixture1.TotalMoles;
            var temperatureRatio = gasMixture2.Temperature / gasMixture1.Temperature;
            var heatCapacityRatio = GetHeatCapacity(gasMixture2) / GetHeatCapacity(gasMixture1);

            // The quadratic equation is solved for the transfer fraction.
            var quadraticA = 1 + volumeRatio;
            var quadraticB = molesRatio - volumeRatio + heatCapacityRatio * (temperatureRatio + volumeRatio);
            var quadraticC = heatCapacityRatio * (molesRatio * temperatureRatio - volumeRatio);

            return (-quadraticB + MathF.Sqrt(quadraticB * quadraticB - 4 * quadraticA * quadraticC)) / (2 * quadraticA);
        }

        /// <summary>
        /// Determines the number of moles that need to be removed from a <see cref="GasMixture"/> to reach a target pressure threshold.
        /// </summary>
        /// <param name="gasMixture">The gas mixture whose moles and properties will be used in the calculation.</param>
        /// <param name="targetPressure">The target pressure threshold to calculate against.</param>
        /// <returns>The difference in moles required to reach the target pressure threshold.</returns>
        /// <remarks>The temperature of the gas is assumed to be not changing due to a free expansion.</remarks>
        public static float MolesToPressureThreshold(GasMixture gasMixture, float targetPressure)
        {
            // Kid named PV = nRT.
            return gasMixture.TotalMoles -
                   targetPressure * gasMixture.Volume / (Atmospherics.R * gasMixture.Temperature);
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
        ///     Compares two TileAtmospheres to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(TileAtmosphere sample, TileAtmosphere otherSample)
        {
            if (sample.AirArchived == null || otherSample.AirArchived == null)
                return GasCompareResult.NoExchange;

            return CompareExchange(sample.AirArchived, otherSample.AirArchived);
        }

        /// <summary>
        ///     Compares two gas mixtures to see if they are within acceptable ranges for group processing to be enabled.
        /// </summary>
        public GasCompareResult CompareExchange(GasMixture sample, GasMixture otherSample)
        {
            var moles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasMoles = sample.Moles[i];
                var delta = MathF.Abs(gasMoles - otherSample.Moles[i]);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                    return (GasCompareResult)i; // We can move gases!
                moles += gasMoles;
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(sample.Temperature - otherSample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return GasCompareResult.TemperatureExchange; // There can be temperature exchange.
            }

            // No exchange at all!
            return GasCompareResult.NoExchange;
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

                    if (!(mixture.GetMoles(i) < req))
                        continue;

                    doReaction = false;
                    break;
                }

                if (!doReaction)
                    continue;

                reaction = prototype.React(mixture, holder, this, HeatScale);
                if(reaction.HasFlag(ReactionResult.StopReactions))
                    break;
            }

            return reaction;
        }

        public enum GasCompareResult
        {
            NoExchange = -2,
            TemperatureExchange = -1,
        }
    }
}
