using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Forms BZ from mixing Plasma and Nitrous Oxide at low pressure. Also decomposes Nitrous Oxide when there are more than 3 parts Plasma per N2O.
/// </summary>
[UsedImplicitly]
public sealed partial class BZFormationReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initN2O = mixture.GetMoles(Gas.NitrousOxide);
        var initPlasma = mixture.GetMoles(Gas.Plasma);
        var pressure = mixture.Pressure;
        var volume = mixture.Volume;

        var environmentEfficiency = volume / pressure; // more volume and less pressure gives better rates
        var ratioEfficiency = Math.Min(initN2O / initPlasma, 1); // less n2o than plasma gives lower rates

        var totalRate = environmentEfficiency * ratioEfficiency / Atmospherics.BZFormationRate;

        var n2oRemoved = totalRate * 2f;
        var plasmaRemoved = totalRate * 4f;
        var bzFormed = totalRate * 5f;

        if (n2oRemoved > initN2O || plasmaRemoved > initPlasma)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.NitrousOxide, -n2oRemoved);
        mixture.AdjustMoles(Gas.Plasma, -plasmaRemoved);
        mixture.AdjustMoles(Gas.BZ, bzFormed);

        var energyReleased = bzFormed * Atmospherics.BZFormationEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
