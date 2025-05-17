using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Defines a gas pressure relief valve,
/// which releases gas depending on a set pressure threshold between two pipe nodes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class GasPressureReliefValveComponent : Component
{
    /// <summary>
    /// Specifies the pipe node name to be treated as the inlet.
    /// </summary>
    [DataField]
    public string InletName = "inlet";

    /// <summary>
    /// Specifies the pipe node name to be treated as the outlet.
    /// </summary>
    [DataField]
    public string OutletName = "outlet";

    /// <summary>
    /// Sets the opening threshold of the valve.
    /// </summary>
    /// <example> If set to 500 kPa, the valve will only
    /// open if the pressure in the inlet side is above
    /// 500 kPa. </example>
    [DataField, AutoNetworkedField]
    public float Threshold;

    /// <summary>
    /// Determines whether the valve is open or closed.
    /// Used for showing the valve animation, the UI,
    /// and on examine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// The max transfer rate of the valve.
    /// </summary>
    [GuidebookData]
    [DataField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// The current flow rate of the valve.
    /// Used for displaying the flow rate in the UI,
    /// and on examine.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float FlowRate;
}
