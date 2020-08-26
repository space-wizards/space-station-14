#nullable enable
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    public class TritiumFireReaction : IGasReactionEffect
    {
        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
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

                    var tile = location.GridIndices.GetTileRef(location.GridIndex);

                    var eventArgs = new TemperatureExposeEventArgs(mixture, temperature, mixture.Volume);

                    if(tile != null)
                        foreach (var entity in tile.Value.GetEntitiesInTile())
                        {
                            foreach (var temp in entity.GetAllComponents<ITemperatureExpose>())
                            {
                                temp.TemperatureExpose(eventArgs);
                            }
                        }

                    location.TemperatureExpose(eventArgs);
                }
            }

            return mixture.ReactionResults[GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
