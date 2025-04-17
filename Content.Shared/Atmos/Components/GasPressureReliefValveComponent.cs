using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Shared component side for the <see cref="SharedGasPressureReliefValveSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class GasPressureReliefValveComponent : Component
{
    [DataField]
    public string InletName = "inlet";

    [DataField]
    public string OutletName = "outlet";

    /// <summary>
    /// Sets the opening threshold of the valve.
    /// <example>If set to 500 kPa, the valve will only
    /// open if the pressure in the inlet side is above
    /// 500 kPa.</example>
    /// </summary>
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
    /// Stores the previous state of the valve. If the current valve state
    /// is different from the previous state, the valve will be dirtied.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public bool PreviousValveState;

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
    [DataField, AutoNetworkedField]
    public float FlowRate;
}
