using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.StatusIcon;

/// <summary>
/// This is used for noting if an entity is able to
/// have StatusIcons displayed on them and inherent icons. (debug purposes)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NonMobStatusIconComponent : Component
{
    /// <summary>
    /// Optional bounds for where the icons are laid out.
    /// If null, the sprite bounds will be used.
    /// </summary>
    [AutoNetworkedField]
    [DataField("bounds"), ViewVariables(VVAccess.ReadWrite)]
    public Box2? Bounds;
}
