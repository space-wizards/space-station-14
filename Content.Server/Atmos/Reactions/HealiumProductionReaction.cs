using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces Healium by mixing BZ and Frezon at temperatures between 23K and 293K. Efficiency increases in colder temperatures.  
/// </summary>
[UsedImplicitly]
public sealed partial class HealiumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initBZ = mixture.GetMoles(Gas.BZ);
        var initFrezon = mixture.GetMoles(Gas.Frezon);

        var rate = mixture.Temperature / Atmospherics.T20C;
        var efficiency = 23.15f / mixture.Temperature;

        var bZRemoved = 1f * rate;
        var frezonRemoved = 11f * rate;
        var healiumProduced = 12f * rate * efficiency;

        if (bZRemoved > initBZ || frezonRemoved > initFrezon || mixture.Temperature > Atmospherics.T20C)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.BZ, -bZRemoved);
        mixture.AdjustMoles(Gas.Frezon, -frezonRemoved);
        mixture.AdjustMoles(Gas.Healium, healiumProduced);

        var energyReleased = healiumProduced * Atmospherics.HealiumProductionEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
