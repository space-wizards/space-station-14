using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasPassiveGateComponent : Component
{
    [DataField("inlet")]
    public string InletName = "inlet";
    [DataField("outlet")]
    public string OutletName = "outlet";

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float FlowRate;

    public float OldFlowRate;
}
