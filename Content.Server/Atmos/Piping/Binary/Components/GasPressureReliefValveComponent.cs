using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Binary.Components;

/// <summary>
/// Component side of the <see cref="GasPressureReliefValveSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class GasPressureReliefValveComponent : Component
{
    [DataField]
    public string InletName = "inlet";

    [DataField]
    public string OutletName = "outlet";

    [DataField]
    public float Threshold;

    /// <summary>
    /// Determines whether the valve is open or closed.
    /// Used for showing the valve animation, the UI,
    /// and on examine.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    /// The max transfer rate of the valve.
    /// </summary>
    [DataField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// The current flow rate of the valve.
    /// Used for displaying the flow rate in the UI,
    /// and on examine.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public float FlowRate;
}
