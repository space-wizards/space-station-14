#nullable enable
using Content.Server.Atmos;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Interfaces
{
    public interface IGasReactionEffect : IExposeData
    {
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder);
    }
}
