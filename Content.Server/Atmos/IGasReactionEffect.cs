using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;

namespace Content.Server.Atmos
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IGasReactionEffect
    {
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem);
    }
}
