using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// Makes the entity immune to being flashed.
/// When given to clothes in the "head", "eyes" or "mask" slot it protects the wearer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFlashSystem))]
public sealed partial class FlashImmunityComponent : Component
{
    /// <summary>
    /// Is this component currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Should the flash protection be shown when examining the entity?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowInExamine = true;
}
