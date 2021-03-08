#nullable enable
using System;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class PlasmaFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, GridTileLookupSystem gridTileLookup)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = mixture.HeatCapacity;
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;

            // More plasma released at higher temperatures
            var temperatureScale = 0f;
            var superSaturation = false;

            if (temperature > Atmospherics.PlasmaUpperTemperature)
                temperatureScale = 1f;
            else
                temperatureScale = (temperature - Atmospherics.PlasmaMinimumBurnTemperature) /
                                   (Atmospherics.PlasmaUpperTemperature - Atmospherics.PlasmaMinimumBurnTemperature);

            if (temperatureScale > 0f)
            {
                var plasmaBurnRate = 0f;
                var oxygenBurnRate = Atmospherics.OxygenBurnRateBase - temperatureScale;

                if (mixture.GetMoles(Gas.Oxygen) / mixture.GetMoles(Gas.Plasma) >
                    Atmospherics.SuperSaturationThreshold)
                    superSaturation = true;

                if (mixture.GetMoles(Gas.Oxygen) >
                    mixture.GetMoles(Gas.Plasma) * Atmospherics.PlasmaOxygenFullburn)
                    plasmaBurnRate = (mixture.GetMoles(Gas.Plasma) * temperatureScale) /
                                     Atmospherics.PlasmaBurnRateDelta;
                else
                    plasmaBurnRate = (temperatureScale * (mixture.GetMoles(Gas.Oxygen) / Atmospherics.PlasmaOxygenFullburn)) / Atmospherics.PlasmaBurnRateDelta;

                if (plasmaBurnRate > Atmospherics.MinimumHeatCapacity)
                {
                    plasmaBurnRate = MathF.Min(MathF.Min(plasmaBurnRate, mixture.GetMoles(Gas.Plasma)), mixture.GetMoles(Gas.Oxygen)/oxygenBurnRate);
                    mixture.SetMoles(Gas.Plasma, mixture.GetMoles(Gas.Plasma) - plasmaBurnRate);
                    mixture.SetMoles(Gas.Oxygen, mixture.GetMoles(Gas.Oxygen) - (plasmaBurnRate * oxygenBurnRate));

                    mixture.AdjustMoles(superSaturation ? Gas.Tritium : Gas.CarbonDioxide, plasmaBurnRate);

                    energyReleased += Atmospherics.FirePlasmaEnergyReleased * (plasmaBurnRate);

                    mixture.ReactionResults[GasReaction.Fire] += (plasmaBurnRate) * (1 + oxygenBurnRate);
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
