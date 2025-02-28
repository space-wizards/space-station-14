using System.Runtime.CompilerServices;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlasmaFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture,
            IGasMixtureHolder? holder,
            AtmosphereSystem atmosphereSystem,
            float heatScale)
        {
            var temperature = mixture.Temperature;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0;

            // React faster and with less oxygen per plasma at higher temperatures, up to a maximum.
            var temperatureScale = ProportionOfRange(temperature,
                Atmospherics.PlasmaMinimumBurnTemperature,
                Atmospherics.PlasmaUpperTemperature);
            if (temperatureScale > 0)
            {
                var initialOxygenMoles = mixture.GetMoles(Gas.Oxygen);
                var initialPlasmaMoles = mixture.GetMoles(Gas.Plasma);

                // The maximum rate based on the relative proportions of reactants, expressed as moles of plasma burned.
                var maximumReactionRate =
                    Math.Min(initialPlasmaMoles, initialOxygenMoles / Atmospherics.PlasmaOxygenFullburn);
                var reactionRate = maximumReactionRate * temperatureScale / Atmospherics.PlasmaBurnRateDelta;

                // TODO I have no fucking idea why the rate is compared against a heat capacity here.
                // ... but it has the effect of not doing little finicky adjustments, I guess.
                if (reactionRate > Atmospherics.MinimumHeatCapacity)
                {
                    var initialThermalEnergy = temperature * atmosphereSystem.GetHeatCapacity(mixture, true);

                    var oxygenPerPlasmaBurned = Atmospherics.OxygenBurnRateBase - temperatureScale;
                    // Don't try to use more of the reactants than are actually present.
                    var reactantLimitedReactionRate = MathF.Min(reactionRate,
                        MathF.Min(initialPlasmaMoles, initialOxygenMoles / oxygenPerPlasmaBurned));
                    mixture.SetMoles(Gas.Plasma, initialPlasmaMoles - reactantLimitedReactionRate);
                    mixture.SetMoles(Gas.Oxygen,
                        initialOxygenMoles - reactantLimitedReactionRate * oxygenPerPlasmaBurned);

                    // Mixes supersaturated with oxygen relative to plasma create some tritium rather than CO2.
                    var supersaturation = ProportionOfRange(initialOxygenMoles / initialPlasmaMoles,
                        Atmospherics.SuperSaturationThreshold,
                        Atmospherics.SuperSaturationEnds);
                    mixture.AdjustMoles(Gas.Tritium, reactantLimitedReactionRate * supersaturation);
                    mixture.AdjustMoles(Gas.CarbonDioxide, reactantLimitedReactionRate * (1.0f - supersaturation));

                    // adjust energy by `heatScale` to make sure speedup doesn't cause mega temperature rise
                    var energyReleased = Atmospherics.FirePlasmaEnergyReleased * reactionRate / heatScale;
                    mixture.ReactionResults[(byte)GasReaction.Fire] = reactionRate * (1 + oxygenPerPlasmaBurned);

                    if (energyReleased > 0)
                    {
                        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                            mixture.Temperature = (initialThermalEnergy + energyReleased) / newHeatCapacity;
                    }
                }
            }

            if (holder is TileAtmosphere location && mixture.Temperature > Atmospherics.FireMinimumTemperatureToExist)
            {
                atmosphereSystem.HotspotExpose(location, mixture.Temperature, mixture.Volume);
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0
                ? ReactionResult.Reacting
                : ReactionResult.NoReaction;
        }

        /// <summary>
        /// Normalizes <paramref name="value"/> as a number in the range of <paramref name="min"/> to <paramref name="max"/>,
        /// returning a value from 0.0, when <paramref name="value"/> is <paramref name="min"/>, to 1.0, when
        /// <paramref name="value"/> is <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ProportionOfRange(float value, float min, float max)
        {
            return Math.Clamp((value - min) / (max - min), 0.0f, 1.0f);
        }
    }
}
