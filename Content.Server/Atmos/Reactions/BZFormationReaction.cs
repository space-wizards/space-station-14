using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Forms BZ from mixing Plasma and Nitrous Oxide at low pressure. Also decomposes Nitrous Oxide when there are more than 3 parts Plasma per N2O.
/// </summary>
[UsedImplicitly]
public sealed partial class BZFormationReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var initN2O = mixture.GetMoles(Gas.NitrousOxide);
        var initPlasma = mixture.GetMoles(Gas.Plasma);
        var pressure = mixture.Pressure;
        var volume = mixture.Volume;
        var environmentEfficiency = volume / pressure; // more volume and less pressure gives better rates
        var ratioEfficiency = Math.Min(initN2O / initPlasma, 1); // less n2o than plasma gives lower rates
        var n2oDecomposeFactor = Math.Max(4 * (initPlasma / initN2O + initPlasma - .75f), 0); // n2o decomposes when there are more than 3 plasma parts per n2o

        var n2oRemoved = initN2O * (1 / .4f);
        var plasmaRemoved = initPlasma * (1 / (.8f * (1 - n2oDecomposeFactor)));
        var bzFormed = Math.Min(Math.Min(.01f * ratioEfficiency * environmentEfficiency, n2oRemoved), plasmaRemoved) / Atmospherics.BZFormationRate;

        /* If n2o-plasma ratio is less than 1:3 start decomposing n2o.
	     * Rate of decomposition vs BZ production increases as n2o concentration gets lower
	     * Plasma acts as a catalyst on decomposition, so it doesn't get consumed in the process.
	     * N2O decomposes with its normal decomposition energy */
        if (n2oDecomposeFactor > 0)
        {
            var amountDecomposed = .4f * bzFormed * n2oDecomposeFactor;
            mixture.AdjustMoles(Gas.Nitrogen, -amountDecomposed);
            mixture.AdjustMoles(Gas.Oxygen, -amountDecomposed * .5f);
            mixture.AdjustMoles(Gas.NitrousOxide, amountDecomposed * 1.5f);
        }

        mixture.AdjustMoles(Gas.BZ, bzFormed * (1 - n2oDecomposeFactor));
        mixture.AdjustMoles(Gas.NitrousOxide, -(.4f * bzFormed));
        mixture.AdjustMoles(Gas.Plasma, -(.8f * bzFormed * (1 - n2oDecomposeFactor)));

        return ReactionResult.Reacting;
    }
}
