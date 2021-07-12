using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Server.Interfaces
{
    public interface IGasMixtureHolder
    {
        public GasMixture Air { get; set; }

        public virtual void AssumeAir(GasMixture giver)
        {
            EntitySystem.Get<AtmosphereSystem>().Merge(Air, giver);
        }

        public GasMixture RemoveAir(float amount)
        {
            return Air.Remove(amount);
        }

        public GasMixture RemoveAirVolume(float ratio)
        {
            return Air.RemoveRatio(ratio);
        }
    }
}
