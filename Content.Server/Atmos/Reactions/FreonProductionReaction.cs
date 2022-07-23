using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Reactions;

public sealed class FreonProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        return ReactionResult.NoReaction;
    }
}
