using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Pumps and mixes gases from the two input ports into the output port, accoring to the configured ratio.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasMixerComponent : Component
{
    /// <summary>
    /// Whether or not the mixer is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Name of the primary inlet port.
    /// </summary>
    [DataField("inletOne")]
    public string InletOneName = "inletOne";

    /// <summary>
    /// Name of the secondary inlet port.
    /// </summary>
    [DataField("inletTwo")]
    public string InletTwoName = "inletTwo";

    /// <summary>
    /// Name of the outlet port.
    /// </summary>
    [DataField("outlet")]
    public string OutletName = "outlet";

    /// <summary>
    /// Pressure that the mixer is set to output.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    /// Max pressure that the mixer can be set to output.
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    /// <summary>
    /// Fraction (0.0 to 1.0) of the gas to take from the primary port.
    /// </summary>
    /// The secondary port fraction is one minus this value.
    [DataField, AutoNetworkedField]
    public float InletOneConcentration = 0.5f;
}
