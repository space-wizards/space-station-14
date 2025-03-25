using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasPressurePumpComponent : Component
{
    [Access(typeof(SharedGasPressurePumpSystem))]
    public AtmosToggleableComponent ToggleableComponent;

    /// <summary>
    ///     The default Enabled value for this comp's AtmosToggleableComponent. Only used on init.
    /// </summary>
    [DataField("enabled"), AutoNetworkedField]
    public bool DefaultEnabled = false;

    [DataField("inlet")]
    public string InletName = "inlet";

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
}
