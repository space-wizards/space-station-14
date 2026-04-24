using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Server.Atmos.Piping.Trinary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
//[Access(typeof(GasMixerSystem))]
public sealed partial class GasMixerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [ViewVariables]
    public string InletOneName = "inletOne";

    [ViewVariables]
    public string InletTwoName = "inletTwo";

    [ViewVariables]
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    [DataField, AutoNetworkedField]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    [DataField, AutoNetworkedField]
    public float InletOneConcentration = 0.5f;

    [DataField, AutoNetworkedField]
    public float InletTwoConcentration = 0.5f;
}
