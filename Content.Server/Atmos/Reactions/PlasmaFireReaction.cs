using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlasmaFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[GasReaction.Fire] = 0;

            // More plasma released at higher temperatures.
            var temperatureScale = 0f;

            if (temperature > Atmospherics.PlasmaUpperTemperature)
                temperatureScale = 1f;
            else
                temperatureScale = (temperature - Atmospherics.PlasmaMinimumBurnTemperature) /
                                   (Atmospherics.PlasmaUpperTemperature - Atmospherics.PlasmaMinimumBurnTemperature);

            if (temperatureScale > 0)
            {
                var oxygenBurnRate = Atmospherics.OxygenBurnRateBase - temperatureScale;
                var plasmaBurnRate = 0f;

                var initialOxygenMoles = mixture.GetMoles(Gas.Oxygen);
                var initialPlasmaMoles = mixture.GetMoles(Gas.Plasma);

                // Supersaturation makes tritium.
                var oxyRatio = initialOxygenMoles / initialPlasmaMoles;
                // Efficiency of reaction decreases from 1% Plasma to 3% plasma:
                var supersaturation = Math.Clamp((oxyRatio - Atmospherics.SuperSaturationEnds) /
                                                 (Atmospherics.SuperSaturationThreshold -
                                                  Atmospherics.SuperSaturationEnds), 0.0f, 1.0f);

                if (initialOxygenMoles > initialPlasmaMoles * Atmospherics.PlasmaOxygenFullburn)
                    plasmaBurnRate = initialPlasmaMoles * temperatureScale / Atmospherics.PlasmaBurnRateDelta;
                else
                    plasmaBurnRate = temperatureScale * (initialOxygenMoles / Atmospherics.PlasmaOxygenFullburn) / Atmospherics.PlasmaBurnRateDelta;

                if (plasmaBurnRate > Atmospherics.MinimumHeatCapacity)
                {
                    plasmaBurnRate = MathF.Min(plasmaBurnRate, MathF.Min(initialPlasmaMoles, initialOxygenMoles / oxygenBurnRate));
                    mixture.SetMoles(Gas.Plasma, initialPlasmaMoles - plasmaBurnRate);
                    mixture.SetMoles(Gas.Oxygen, initialOxygenMoles - plasmaBurnRate * oxygenBurnRate);

                    // supersaturation adjusts the ratio of produced tritium to unwanted CO2
                    mixture.AdjustMoles(Gas.Tritium, plasmaBurnRate * supersaturation);
                    mixture.AdjustMoles(Gas.CarbonDioxide, plasmaBurnRate * (1.0f - supersaturation));

                    energyReleased += Atmospherics.FirePlasmaEnergyReleased * plasmaBurnRate;
                    mixture.ReactionResults[GasReaction.Fire] += plasmaBurnRate * (1 + oxygenBurnRate);
                }
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;
            }

            if (location != null)
            {
                var mixTemperature = mixture.Temperature;
                if (mixTemperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(location.GridIndex, location.GridIndices, mixTemperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
