using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Piping.Components;

/// <summary>
///     Component to hold data for whether an atmos device is toggled on, and also allows alternative verbs for toggling them.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AtmosToggleableComponent : Component
{
    /// <summary>
    ///     If true, this device can be interacted with via alt verbs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool AltVerbAvailable = false;

    /// <summary>
    ///     If true, this device is toggled on and should be enabled whenever possible (e.g., whenever it has power and is anchored.)
    ///     This is usually set by init of another component of this component's object to match DefaultEnabled var.
    /// </summary>
    /// <remarks>
    ///     Should never be directly set outside of initialisation (including YAML, but not always a good idea!) and events.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Enabled = false;

    /// <summary>
    ///     Loc id for the text that shows this device's alternative verb in the right click menu.
    ///     Useless if AltVerbAvailable is false.
    /// </summary>
    public readonly string Text = "atmos-toggleable-component-toggle-verb";

    /// <summary>
    ///     Loc id for the text that shows this device's alternative verb in the right click menu.
    ///     Useless if AltVerbAvailable is false.
    /// </summary>
    public readonly SpriteSpecifier? Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png"));
}
