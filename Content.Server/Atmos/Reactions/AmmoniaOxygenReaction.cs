using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class AmmoniaOxygenReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var nAmmonia = mixture.GetMoles(Gas.Ammonia);
        var nOxygen = mixture.GetMoles(Gas.Oxygen);
        var nTotal = mixture.TotalMoles;

        // Concentration-dependent reaction rate
        var fAmmonia = nAmmonia/nTotal;
        var fOxygen = nOxygen/nTotal;
        var rate = MathF.Pow(fAmmonia, 2) * MathF.Pow(fOxygen, 2);

        var deltaMoles = nAmmonia / Atmospherics.AmmoniaOxygenReactionRate * 2 * rate;

        if (deltaMoles <= 0 || nAmmonia - deltaMoles < 0)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Ammonia, -deltaMoles);
        mixture.AdjustMoles(Gas.Oxygen, -deltaMoles);
        mixture.AdjustMoles(Gas.NitrousOxide, deltaMoles / 2);
        mixture.AdjustMoles(Gas.WaterVapor, deltaMoles * 1.5f);

        return ReactionResult.Reacting;
    }
}
