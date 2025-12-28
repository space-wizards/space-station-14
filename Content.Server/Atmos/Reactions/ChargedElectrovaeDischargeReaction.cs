using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Marks tiles with charged electrovae for discharge effects.
///     The actual effects (power effects and lightning strikes) are handled by AtmosphereSystem.
/// </summary>
[UsedImplicitly]
public sealed partial class ChargedElectrovaeDischargeReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        const float intensityDivisor = 2f;
        const float dischargeRate = 0.025f;

        var initialCE = mixture.GetMoles(Gas.ChargedElectrovae);
        if (holder is not TileAtmosphere tileAtmos)
            return ReactionResult.NoReaction;

        var intensity = Math.Min(initialCE / intensityDivisor, 1f);
        atmosphereSystem.ChargedElectrovaeExpose(tileAtmos.GridIndex, tileAtmos, intensity);

        // Slowly discharge: ChargedElectrovae -> Electrovae
        var dischargeAmount = Math.Min(initialCE * dischargeRate, initialCE);
        mixture.AdjustMoles(Gas.ChargedElectrovae, -dischargeAmount);
        mixture.AdjustMoles(Gas.Electrovae, dischargeAmount);

        return ReactionResult.Reacting;
    }
}
