using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces frezon from oxygen and bz, with nitrogen as a catalyst that also acts as a stopper if too much is present.
///     Has a max temperature, but paradoxically gets more efficient the hotter it is.
/// </summary>
[UsedImplicitly]
public sealed partial class FrezonProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialOxy = mixture.GetMoles(Gas.Oxygen);
        var initialBZ = mixture.GetMoles(Gas.BZ);

        var efficiency = mixture.Temperature / Atmospherics.FrezonProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        // How much the catalyst (N2) will allow us to produce
        // Less N2 is required the more efficient it is.
        var catalystLimit = initialN2 * (Atmospherics.FrezonProductionNitrogenRatio / efficiency);
        var oxyLimit = Math.Min(initialOxy, catalystLimit) / Atmospherics.FrezonProductionBZRatio;

        // Amount of bz & oxygen that are reacting
        var bzBurned = Math.Min(oxyLimit, initialBZ);
        var oxyBurned = bzBurned * Atmospherics.FrezonProductionBZRatio;

        var oxyConversion = oxyBurned / Atmospherics.FrezonProductionConversionRate;
        var bzConversion = bzBurned / Atmospherics.FrezonProductionConversionRate;
        var total = oxyConversion + bzConversion;

        mixture.AdjustMoles(Gas.Oxygen, -oxyConversion);
        mixture.AdjustMoles(Gas.BZ, -bzConversion);
        mixture.AdjustMoles(Gas.Frezon, total * efficiency);
        mixture.AdjustMoles(Gas.Nitrogen, total * loss);

        return ReactionResult.Reacting;
    }
}
