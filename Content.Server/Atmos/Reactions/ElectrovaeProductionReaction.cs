using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces electrovae from water vapor and nitrous oxide;
///     with nitrogen as a catalyst, that also acts as a stopper if too much is present.
///     This reaction is temperature-dependent, with higher temperatures increasing efficiency.
///     Nitrogen acts as both a catalyst (enabling the reaction) and a limiter (preventing runaway production).
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialN2O = mixture.GetMoles(Gas.NitrousOxide);
        var initialH2O = mixture.GetMoles(Gas.WaterVapor);

        var efficiency = mixture.Temperature / Atmospherics.ElectrovaeProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        // How much the catalyst (N2) will allow us to produce
        // Less N2 is required the more efficient it is.
        var catalystLimit = initialN2 * (Atmospherics.ElectrovaeProductionNitrogenRatio / efficiency);
        var n2oLimit = Math.Min(initialN2O, catalystLimit) / Atmospherics.ElectrovaeProductionWaterVaporRatio;

        // Amount of water vapor & nitrous oxide that are reacting
        var h2oBurned = Math.Min(n2oLimit, initialH2O);
        var n2oBurned = h2oBurned * Atmospherics.ElectrovaeProductionWaterVaporRatio;

        var n2oConversion = n2oBurned / Atmospherics.ElectrovaeProductionConversionRate;
        var h2oConversion = h2oBurned / Atmospherics.ElectrovaeProductionConversionRate;
        var total = n2oConversion + h2oConversion;

        mixture.AdjustMoles(Gas.NitrousOxide, -n2oConversion);
        mixture.AdjustMoles(Gas.WaterVapor, -h2oConversion);
        mixture.AdjustMoles(Gas.Electrovae, total * efficiency);
        mixture.AdjustMoles(Gas.Nitrogen, total * loss);

        return ReactionResult.Reacting;
    }
}
