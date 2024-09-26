using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces Healium by mixing BZ and Frezon at temperatures between 23K and 300K. Efficiency increases in colder temperatures.  
/// </summary>
[UsedImplicitly]
public sealed partial class HealiumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initBZ = mixture.GetMoles(Gas.BZ);
        var initFrezon = mixture.GetMoles(Gas.Frezon);
        var pressure = mixture.Pressure;
        var volume = mixture.Volume;

        var efficiency = mixture.Temperature / 24f;


        var bZRemoved = 1f;
        var frezonRemoved = 11f;
        var healiumProduced = 12f * efficiency;

        if (bZRemoved > initBZ || frezonRemoved > initFrezon)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.BZ, -bZRemoved);
        mixture.AdjustMoles(Gas.Frezon, -frezonRemoved);
        mixture.AdjustMoles(Gas.Healium, healiumProduced);

        var energyReleased = healiumProduced * Atmospherics.HealiumProductionEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.T20C);

        return ReactionResult.Reacting;
    }
}
