#nullable enable
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    public class TritiumFireReaction : IGasReactionEffect
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
        }

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, GridTileLookupSystem gridTileLookup)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = mixture.HeatCapacity;
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[GasReaction.Fire] = 0f;
            var burnedFuel = 0f;
            var initialTrit = mixture.GetMoles(Gas.Tritium);

            if (mixture.GetMoles(Gas.Oxygen) < initialTrit ||
                Atmospherics.MinimumTritiumOxyburnEnergy > (temperature * oldHeatCapacity))
            {
                burnedFuel = mixture.GetMoles(Gas.Oxygen) / Atmospherics.TritiumBurnOxyFactor;
                if (burnedFuel > initialTrit)
                    burnedFuel = initialTrit;

                mixture.AdjustMoles(Gas.Tritium, -burnedFuel);
            }
            else
            {
                burnedFuel = initialTrit;
                mixture.SetMoles(Gas.Tritium, mixture.GetMoles(Gas.Tritium ) * (1 - 1 / Atmospherics.TritiumBurnTritFactor));
                mixture.AdjustMoles(Gas.Oxygen, -mixture.GetMoles(Gas.Tritium));
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel * (Atmospherics.TritiumBurnTritFactor - 1));
            }

            if (burnedFuel > 0)
            {
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel);

                // TODO ATMOS Radiation pulse here!

                // Conservation of mass is important.
                mixture.AdjustMoles(Gas.WaterVapor, burnedFuel);

                mixture.ReactionResults[GasReaction.Fire] += burnedFuel;
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
                    location.HotspotExpose(temperature, mixture.Volume);

                    foreach (var entity in location.GridIndices.GetEntitiesInTileFast(location.GridIndex, gridTileLookup))
                    {
                        foreach (var temperatureExpose in entity.GetAllComponents<ITemperatureExpose>())
                        {
                            temperatureExpose.TemperatureExpose(mixture, temperature, mixture.Volume);
                        }
                    }

                    location.TemperatureExpose(mixture, temperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
