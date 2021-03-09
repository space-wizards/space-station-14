#nullable enable
using Content.Server.Atmos;
using Content.Server.Atmos.Reactions;
using Robust.Server.GameObjects;
using Robust.Shared.EntityLookup;

namespace Content.Server.Interfaces
{
    public interface IGasReactionEffect
    {
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, SharedEntityLookupSystem gridTileLookup);
    }
}
