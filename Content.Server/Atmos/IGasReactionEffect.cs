using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;

namespace Content.Server.Atmos
{
    [ImplicitDataDefinitionForInheritors]
    public partial interface IGasReactionEffect
    {
        /// <summary>
        /// Process this reaction effect.
        /// </summary>
        /// <param name="mixture">The gas mixture to react</param>
        /// <param name="holder">The container of this gas mixture</param>
        /// <param name="atmosphereSystem">The atmosphere system</param>
        /// <param name="heatScale">Scaling factor that should be applied to all heat input or outputs.</param>
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem,
            float heatScale);
    }
}
