using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
///     Makes the entity immune to being flashed.
///     When given to clothes in the "head", "eyes" or "mask" slot it protects the wearer.
/// </summary>
[RegisterComponent, Access(typeof(SharedFlashSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashImmunityComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;

    //starlight
    /// <summary>
    /// If true, will affect night vision, thermal vision, and cyclorite vision.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [DataField]
    public bool BlocksSpecialVision { get; set; } = true;
    //starlight end
}
