using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class MiasmaOxygenReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var cacheMiasma = mixture.GetMoles(Gas.Miasma);

        var deltaMoles = cacheMiasma / Atmospherics.MiasmaOxygenReactionRate * 2;

        if (deltaMoles <= 0 || cacheMiasma - deltaMoles < 0)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Miasma, -deltaMoles);
        mixture.AdjustMoles(Gas.Oxygen, -deltaMoles);
        mixture.AdjustMoles(Gas.NitrousOxide, deltaMoles / 2);
        mixture.AdjustMoles(Gas.WaterVapor, deltaMoles * 1.5f);

        return ReactionResult.Reacting;
    }
}
