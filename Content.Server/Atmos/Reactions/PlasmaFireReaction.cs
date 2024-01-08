using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PhoronFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[GasReaction.Fire] = 0;

            // More phoron released at higher temperatures.
            var temperatureScale = 0f;

            if (temperature > Atmospherics.PhoronUpperTemperature)
                temperatureScale = 1f;
            else
            {
                temperatureScale = (temperature - Atmospherics.PhoronMinimumBurnTemperature) /
                                   (Atmospherics.PhoronUpperTemperature - Atmospherics.PhoronMinimumBurnTemperature);
            }

            if (temperatureScale > 0)
            {
                var oxygenBurnRate = Atmospherics.OxygenBurnRateBase - temperatureScale;
                var phoronBurnRate = 0f;

                var initialOxygenMoles = mixture.GetMoles(Gas.Oxygen);
                var initialPhoronMoles = mixture.GetMoles(Gas.Phoron);

                // Supersaturation makes tritium.
                var oxyRatio = initialOxygenMoles / initialPhoronMoles;
                // Efficiency of reaction decreases from 1% Phoron to 3% phoron:
                var supersaturation = Math.Clamp((oxyRatio - Atmospherics.SuperSaturationEnds) /
                                                 (Atmospherics.SuperSaturationThreshold -
                                                  Atmospherics.SuperSaturationEnds), 0.0f, 1.0f);

                if (initialOxygenMoles > initialPhoronMoles * Atmospherics.PhoronOxygenFullburn)
                    phoronBurnRate = initialPhoronMoles * temperatureScale / Atmospherics.PhoronBurnRateDelta;
                else
                    phoronBurnRate = temperatureScale * (initialOxygenMoles / Atmospherics.PhoronOxygenFullburn) / Atmospherics.PhoronBurnRateDelta;

                if (phoronBurnRate > Atmospherics.MinimumHeatCapacity)
                {
                    phoronBurnRate = MathF.Min(phoronBurnRate, MathF.Min(initialPhoronMoles, initialOxygenMoles / oxygenBurnRate));
                    mixture.SetMoles(Gas.Phoron, initialPhoronMoles - phoronBurnRate);
                    mixture.SetMoles(Gas.Oxygen, initialOxygenMoles - phoronBurnRate * oxygenBurnRate);

                    // supersaturation adjusts the ratio of produced tritium to unwanted CO2
                    mixture.AdjustMoles(Gas.Tritium, phoronBurnRate * supersaturation);
                    mixture.AdjustMoles(Gas.CarbonDioxide, phoronBurnRate * (1.0f - supersaturation));

                    energyReleased += Atmospherics.FirePhoronEnergyReleased * phoronBurnRate;
                    energyReleased /= heatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
                    mixture.ReactionResults[GasReaction.Fire] += phoronBurnRate * (1 + oxygenBurnRate);
                }
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
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
