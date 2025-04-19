using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Guidebook;
using Content.Shared.Toggleable;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasPressurePumpComponent : Component
{
    [Access(typeof(SharedGasPressurePumpSystem))]
    public ToggleableComponent ToggleableComponent;

    /// <summary>
    ///     The default Enabled value for this comp's ToggleableComponent. Only used on init.
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
