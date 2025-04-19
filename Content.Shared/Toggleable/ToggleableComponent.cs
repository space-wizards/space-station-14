using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Toggleable;

/// <summary>
///     Component to help handle toggling things with <see cref="ToggleableEnabledEvent"/>, <see cref="ToggleableDisabledEvent"/>
///     and with an optional alternate verb.
/// </summary>
/// <remarks>
///     Use <c>ItemToggleComponent</c> for items.
/// </remarks>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableComponent : Component
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
    ///     Should never be directly set outside of initialisation (Using YAML is fine too, but not always a good idea!) and when .
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Enabled = false;

    /// <summary>
    ///     Loc id for the text that shows this device's alternative verb in the context menu.
    ///     Unused if AltVerbAvailable is false.
    /// </summary>
    public readonly string Text = "toggleable-component-toggle-verb-default";

    /// <summary>
    ///     Icon that shows for the device's alternative verb in the context menu.
    ///     Unused if AltVerbAvailable is false.
    /// </summary>
    public readonly SpriteSpecifier? Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png"));
}
