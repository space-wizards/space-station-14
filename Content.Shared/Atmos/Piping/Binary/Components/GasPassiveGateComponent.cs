using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasPassiveGateComponent : Component
{
    [DataField("inlet")]
    public string InletName { get; set; } = "inlet";
    [DataField("outlet")]
    public string OutletName { get; set; } = "outlet";

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public float FlowRate { get; set; } = 0;
}
