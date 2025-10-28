using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasPassiveGateComponent : Component
{
    /// <summary>
    /// Specifies the pipe node name to be treated as the inlet.
    /// </summary>
    [DataField("inlet")]
    public string InletName = "inlet";

    /// <summary>
    /// Specifies the pipe node name to be treated as the outlet.
    /// </summary>
    [DataField("outlet")]
    public string OutletName = "outlet";

    /// <summary>
    /// The current flow rate of the passive gate.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float FlowRate;

    /// <summary>
    /// The previous flow rate of the passive gate.
    /// This is used to determine if the FlowRate field should be dirtied.
    /// </summary>
    public float OldFlowRate;
}
