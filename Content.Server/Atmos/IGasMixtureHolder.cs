using Content.Server.Atmos.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos
{
    public interface IGasMixtureHolder
    {
        public GasMixture Air { get; set; }
    }
}
