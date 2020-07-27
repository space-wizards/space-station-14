using CannyFastMath;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos
{
    public class PhoronFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = mixture.HeatCapacity;
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;

            // More phoron released at higher temperatures
            var temperatureScale = 0f;
            var superSaturation = false;

            if (temperature > Atmospherics.PhoronUpperTemperature)
                temperatureScale = 1f;
            else
                temperatureScale = (temperature - Atmospherics.PhoronMinimumBurnTemperature) /
                                   (Atmospherics.PhoronUpperTemperature - Atmospherics.PhoronMinimumBurnTemperature);

            if (temperatureScale > 0f)
            {
                var phoronBurnRate = 0f;
                var oxygenBurnRate = Atmospherics.OxygenBurnRateBase - temperatureScale;

                if (mixture.GetMoles(Gas.Oxygen) / mixture.GetMoles(Gas.Phoron) >
                    Atmospherics.SuperSaturationThreshold)
                    superSaturation = true;

                if (mixture.GetMoles(Gas.Oxygen) >
                    mixture.GetMoles(Gas.Phoron) * Atmospherics.PhoronOxygenFullburn)
                    phoronBurnRate = (mixture.GetMoles(Gas.Phoron) * temperatureScale) /
                                     Atmospherics.PhoronBurnRateDelta;
                else
                    phoronBurnRate = (temperatureScale * (mixture.GetMoles(Gas.Oxygen) / Atmospherics.PhoronOxygenFullburn)) / Atmospherics.PhoronBurnRateDelta;

                if (phoronBurnRate > Atmospherics.MinimumHeatCapacity)
                {
                    phoronBurnRate = MathF.Min(MathF.Min(phoronBurnRate, mixture.GetMoles(Gas.Phoron)), mixture.GetMoles(Gas.Oxygen)/oxygenBurnRate);
                    mixture.SetMoles(Gas.Phoron, mixture.GetMoles(Gas.Phoron) - phoronBurnRate);
                    mixture.SetMoles(Gas.Oxygen, mixture.GetMoles(Gas.Oxygen) - (phoronBurnRate * oxygenBurnRate));

                    if(superSaturation)
                        mixture.AdjustMoles(Gas.Tritium, phoronBurnRate);
                    else
                        mixture.AdjustMoles(Gas.CarbonDioxide, phoronBurnRate);

                    energyReleased += Atmospherics.FirePhoronEnergyReleased * (phoronBurnRate);

                    mixture.ReactionResultFire += (phoronBurnRate) * (1 + oxygenBurnRate);
                }
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = mixture.HeatCapacity;
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = ((temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);
            }

            if (location != null)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    location.HotspotExpose(temperature, Atmospherics.CellVolume);

                    // TODO ATMOS Expose temperature all items on cell

                    location.TemperatureExpose(mixture, temperature, Atmospherics.CellVolume);
                }
            }

            return mixture.ReactionResultFire != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }

        public void ExposeData(ObjectSerializer serializer)
        {
        }
    }
}
