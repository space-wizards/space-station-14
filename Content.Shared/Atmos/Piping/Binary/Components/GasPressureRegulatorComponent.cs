using Content.Shared.Guidebook;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Atmos.Piping.Binary.Components;

/// <summary>
/// Defines a gas pressure regulator,
/// which releases gas depending on a set pressure threshold between two pipe nodes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, true), AutoGenerateComponentPause]
public sealed partial class GasPressureRegulatorComponent : Component
{
    /// <summary>
    /// Determines whether the valve is open or closed.
    /// Used for showing the valve animation, the UI,
    /// and on examine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

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
    /// The max transfer rate of the pressure regulator.
    /// </summary>
    [GuidebookData]
    [DataField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    /// <summary>
    /// The server time at which the next UI update will be sent.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUiUpdate = TimeSpan.Zero;

    /// <summary>
    /// Sets the opening threshold of the pressure regulator.
    /// </summary>
    /// <example> If set to 500 kPa, the regulator will only
    /// open if the pressure in the inlet side is above
    /// 500 kPa. </example>
    [DataField, AutoNetworkedField]
    public float Threshold;

    /// <summary>
    /// How often the UI update is sent.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    #region UI/Examine Info

    /// <summary>
    /// The current flow rate of the pressure regulator.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float FlowRate;

    /// <summary>
    /// Current inlet pressure the pressure regulator.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float InletPressure;

    /// <summary>
    /// Current outlet pressure of the pressure regulator.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public float OutletPressure;

    #endregion
}
