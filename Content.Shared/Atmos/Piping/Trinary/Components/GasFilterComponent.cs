using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class GasFilterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Node name for the inlet pipe of the filter
    /// </summary>
    [DataField]
    public string Inlet = "inlet";

    /// <summary>
    /// Node name for the primary outlet pipe of the filter
    /// </summary>
    [DataField]
    public string Outlet = "outlet";

    /// <summary>
    /// Node name for the secondary outlet of the filter (gas being filtered)
    /// </summary>
    [DataField]
    public string Filter = "filter";

    [DataField, AutoNetworkedField]
    public float TransferRate = Atmospherics.MaxTransferRate;

    [DataField, AutoNetworkedField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    [DataField, AutoNetworkedField]
    public Gas? FilteredGas;
}
