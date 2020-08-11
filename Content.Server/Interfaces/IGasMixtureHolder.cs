using Content.Server.Atmos;

namespace Content.Server.Interfaces
{
    public interface IGasMixtureHolder
    {
        public GasMixture Air { get; set; }

        bool AssumeAir(GasMixture giver);
    }
}
