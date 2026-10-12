using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

/// <summary>
/// Defines a gas mixer, which mixes two input mixtures with a given proportion
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class GasMixerComponent : Component
{
    /// <summary>
    /// Indicates whether this mixer is currently operational
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Node name for the primary input pipe of the mixer
    /// </summary>
    [DataField]
    public string InletOne = "inletOne";

    /// <summary>
    /// Node name for the secondary input pipe of the mixer
    /// </summary>
    [DataField]
    public string InletTwo = "inletTwo";

    /// <summary>
    /// Node name for the outlet pipe of the mixer
    /// </summary>
    [DataField]
    public string Outlet = "outlet";

    /// <summary>
    /// Outlet node gas pressure, in kPa
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    /// Maximum allowed outlet node gas pressure, in kPa
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    /// <summary>
    /// Proportion of <see cref="InletOne"/> mixture in the <see cref="Outlet"/> pipe
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InletOneConcentration = 0.5f;

    /// <summary>
    /// Proportion of <see cref="InletTwo"/> mixture in the <see cref="Outlet"/> pipe
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InletTwoConcentration = 0.5f;
}
