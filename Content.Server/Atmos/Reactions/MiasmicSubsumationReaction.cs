using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Converts freon into miasma when the two come into contact. Does not occur at very high temperatures.
/// </summary>
[UsedImplicitly]
public sealed class MiasmicSubsumationReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var initialMiasma = mixture.GetMoles(Gas.Miasma);
        var initialFreon = mixture.GetMoles(Gas.Freon);

        var convert = Math.Min(Math.Min(initialFreon, initialMiasma), Atmospherics.MiasmicSubsumationMaxConversionRate);

        mixture.AdjustMoles(Gas.Miasma, convert);
        mixture.AdjustMoles(Gas.Freon, -convert);

        return ReactionResult.NoReaction;
    }
}
