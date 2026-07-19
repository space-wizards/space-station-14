using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class GasMixerComponent : Component
{
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

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    [DataField, AutoNetworkedField]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    [DataField, AutoNetworkedField]
    public float InletOneConcentration = 0.5f;

    [DataField, AutoNetworkedField]
    public float InletTwoConcentration = 0.5f;
}
