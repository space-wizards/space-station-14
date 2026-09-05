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

    /// <summary>
    /// Snapshot of <see cref="Air"/> from just after the pipenet mix, shown in the UI.
    /// A patient inside heats the real mixture back up between ticks, so reading it live makes the readout jump around.
    /// </summary>
    [ViewVariables]
    public GasMixture? UiSample;
}
