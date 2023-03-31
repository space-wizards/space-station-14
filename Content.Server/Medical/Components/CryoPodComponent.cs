using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Medical.Cryogenics;

namespace Content.Server.Medical.Components;

[RegisterComponent]
public sealed class CryoPodComponent: SharedCryoPodComponent
{
    /// <summary>
    /// Local air buffer that will be mixed with the pipenet, if one exists, per tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gasMixture")]
    public GasMixture Air { get; set; } = new(Atmospherics.OneAtmosphere);
}
