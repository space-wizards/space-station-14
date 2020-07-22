using Content.Server.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Interfaces
{
    public interface IGasReactionEffect
    {
        void React(GasMixture mixture, GridCoordinates coordinates);
    }
}
