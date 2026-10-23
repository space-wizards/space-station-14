using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TritiumFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale, float deltaTime)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0f;
            float burnedFuel;
            var initialTrit = mixture.GetMoles(Gas.Tritium);
            var initialOxy = mixture.GetMoles(Gas.Oxygen);

            // Limit the amount of fuel burned by the limiting reactant, either our initial tritium or the amount of oxygen available given the burn ratio.
            // Need at least
            burnedFuel = Atmospherics.TritiumBurnFuelRatio * initialOxy < initialTrit ? Atmospherics.TritiumBurnFuelRatio * initialOxy : initialTrit;
            burnedFuel = burnedFuel * deltaTime / Atmospherics.TritiumBurnTritFactor;
            mixture.AdjustMoles(Gas.Tritium, -burnedFuel);
            mixture.AdjustMoles(Gas.Oxygen, -burnedFuel / Atmospherics.TritiumBurnFuelRatio);

            if (burnedFuel > 0)
            {
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel);

                // TODO ATMOS Radiation pulse here!

                // Conservation of mass is important.
                mixture.AdjustMoles(Gas.WaterVapor, burnedFuel);

                mixture.ReactionResults[(byte)GasReaction.Fire] += burnedFuel;
            }

            energyReleased /= heatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = ((temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);
            }

            if (holder is TileAtmosphere tile)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(tile, temperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
