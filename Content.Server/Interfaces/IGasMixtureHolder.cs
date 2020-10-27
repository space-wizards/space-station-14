using Content.Server.Atmos;

namespace Content.Server.Interfaces
{
    public interface IGasMixtureHolder
    {
        public GasMixture Air { get; set; }

        void AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);
        }

        GasMixture RemoveAir(float amount)
        {
            return Air.Remove(amount);
        }

        GasMixture RemoveAirVolume(float ratio)
        {
            return Air.RemoveRatio(ratio);
        }
    }
}
