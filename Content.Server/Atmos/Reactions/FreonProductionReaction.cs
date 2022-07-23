using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces freon from oxygen and tritium, with nitrogen as a catalyst that also acts as a stopper if too much is present.
///     Has a max temperature, but paradoxically gets more efficient the hotter it is.
/// </summary>
[UsedImplicitly]
public sealed class FreonProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialOxy = mixture.GetMoles(Gas.Oxygen);
        var initialTrit = mixture.GetMoles(Gas.Tritium);

        var efficiency = mixture.Temperature / Atmospherics.FreonProductionMaxEfficiencyTemperature;
        var loss = 1 - efficiency;

        // Less N2 is required the more efficient it is.
        var minimumN2 = (initialOxy + initialTrit) / (Atmospherics.FreonProductionNitrogenRatio * efficiency);

        if (initialN2 < minimumN2)
            return ReactionResult.NoReaction;

        var oxyConversion = initialOxy / Atmospherics.FreonProductionConversionRate;
        var tritConversion = initialTrit / Atmospherics.FreonProductionConversionRate;
        var total = oxyConversion + tritConversion;

        mixture.AdjustMoles(Gas.Oxygen, -oxyConversion);
        mixture.AdjustMoles(Gas.Tritium, -tritConversion);
        mixture.AdjustMoles(Gas.Freon, total * efficiency);
        mixture.AdjustMoles(Gas.Nitrogen, total * loss);

        return ReactionResult.Reacting;
    }
}
