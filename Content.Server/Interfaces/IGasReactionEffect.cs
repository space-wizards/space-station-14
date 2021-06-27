#nullable enable
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;
using Robust.Server.GameObjects;

namespace Content.Server.Interfaces
{
    public interface IGasReactionEffect
    {
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem);
    }
}
