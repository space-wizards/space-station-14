using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasMixerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField("inletOne")]
    public string InletOneName = "inletOne";

    [DataField("inletTwo")]
    public string InletTwoName = "inletTwo";

    [DataField("outlet")]
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Max pressure of the target gas (NOT relative to source).
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    [DataField, AutoNetworkedField]
    public float InletOneConcentration = 0.5f;

    [DataField, AutoNetworkedField]
    public float InletTwoConcentration = 0.5f;
}
