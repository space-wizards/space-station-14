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
        // Exponential decay constant for ChargedElectrovae -> Electrovae conversion.
        // See: https://en.wikipedia.org/wiki/Exponential_decay
        // This value must be between 0 and 1 (0 = no decay, 1 = instant decay).
        const float decayConstant = 0.025f;

        var initialCE = mixture.GetMoles(Gas.ChargedElectrovae);
        if (holder is not TileAtmosphere tileAtmos)
            return ReactionResult.NoReaction;

        var intensity = Math.Min(initialCE / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);
        atmosphereSystem.ChargedElectrovaeExpose(tileAtmos.GridIndex, tileAtmos, intensity);

        // Slowly discharge via exponential decay: ChargedElectrovae -> Electrovae
        var decayAmount = initialCE * decayConstant;
        mixture.AdjustMoles(Gas.ChargedElectrovae, -decayAmount);
        mixture.AdjustMoles(Gas.Electrovae, decayAmount);

        return ReactionResult.Reacting;
    }
}
