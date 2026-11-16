using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

/// <summary>
/// Defines a gas filter, which removes a specific gas out of the input mixture, and outputs that to a secondary outlet
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class GasFilterComponent : Component
{
    /// <summary>
    /// Indicates whether this filter is currently operational
    /// </summary>
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

    /// <summary>
    /// Inlet node gas transfer rate, in L\s
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TransferRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// Maximum allowed inlet node gas transfer rate, in L\s
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// Indicates gas type to be filtered out into the secondary outlet
    /// </summary>
    [DataField, AutoNetworkedField]
    public Gas? FilteredGas;
}
