using Content.Server.Atmos;

namespace Content.Server.Interfaces
{
    public interface IGasMixtureHolder
    {
        public GasMixture Air { get; set; }

        public void AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);
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
