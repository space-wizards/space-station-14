using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasFilterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField("inlet")]
    public string InletName = "inlet";

    [DataField("filter")]
    public string FilterName = "filter";

    [DataField("outlet")]
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField]
    public float TransferRate = Atmospherics.MaxTransferRate;

    [DataField, AutoNetworkedField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    [DataField, AutoNetworkedField]
    public Gas? FilteredGas;
}
