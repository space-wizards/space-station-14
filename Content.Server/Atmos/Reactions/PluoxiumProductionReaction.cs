using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces pluoxium from oxygen, carbon dioxide and tritium.
///     Oxygenates organs with 8 times more effiency as oxygen
/// </summary>
[UsedImplicitly]
public sealed class PluoxiumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var initialCO2 = mixture.GetMoles(Gas.CarbonDioxide);
        var initialOxy = mixture.GetMoles(Gas.Oxygen);
        var initialTrit = mixture.GetMoles(Gas.Tritium);

        var efficiency = mixture.Temperature / Atmospherics.PluoxiumProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        var oxyConversion = initialOxy / Atmospherics.PluoxiumProductionConversionRate;
        var tritConversion = initialTrit / Atmospherics.PluoxiumProductionConversionRate;
        var CO2Conversion = initialCO2 / Atmospherics.PluoxiumProductionCO2ConversionRate;
        var total = oxyConversion + tritConversion + CO2Conversion;

        mixture.AdjustMoles(Gas.Oxygen, -oxyConversion * loss);
        mixture.AdjustMoles(Gas.Tritium, -tritConversion);
        mixture.AdjustMoles(Gas.Pluoxium, total * efficiency);
        mixture.AdjustMoles(Gas.CarbonDioxide, -CO2Conversion);

        return ReactionResult.Reacting;
    }
}
