using Content.Shared.Atmos;

namespace Content.Server.Plasmaman;

[RegisterComponent]
public sealed partial class PlasmamanOxygenIgnitionComponent : Component
{
    [DataField]
    public Gas Gas = Gas.Oxygen;

    [DataField]
    public float MolesToIgnite = 0.5f;

    [DataField]
    public float FireStacksPerUpdate = 0.33f;
}
