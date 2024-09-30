using Content.Server.Atmos;
using Content.Shared.Atmos;

namespace Content.Server.Medical.Components;

[RegisterComponent]
public sealed partial class CryoPodAirComponent : Component
{
    /// <summary>
    /// Local air buffer that will be mixed with the pipenet, if one exists, per tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gasMixture")]
    public GasMixture Air { get; set; } = new GasMixture(1000f);
}
