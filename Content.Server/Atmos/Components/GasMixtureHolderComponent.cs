using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        [DataField] public GasMixture Air { get; set; } = new GasMixture;
    }
}
