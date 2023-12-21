using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class MiasmaOxygenReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var nMiasma = mixture.GetMoles(Gas.Miasma);
        var nOxygen = mixture.GetMoles(Gas.Oxygen);
        var nTotal = mixture.TotalMoles;

        // Concentration-dependent reaction rate
        var fMiasma = nMiasma/nTotal;
        var fOxygen = nOxygen/nTotal;
        var rate = MathF.Pow(fMiasma, 2) * MathF.Pow(fOxygen, 2);

        var deltaMoles = nMiasma / Atmospherics.MiasmaOxygenReactionRate * 2 * rate;

        if (deltaMoles <= 0 || nMiasma - deltaMoles < 0)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Miasma, -deltaMoles);
        mixture.AdjustMoles(Gas.Oxygen, -deltaMoles);
        mixture.AdjustMoles(Gas.NitrousOxide, deltaMoles / 2);
        mixture.AdjustMoles(Gas.WaterVapor, deltaMoles * 1.5f);

        return ReactionResult.Reacting;
    }
}
