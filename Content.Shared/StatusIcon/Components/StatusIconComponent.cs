using Robust.Shared.GameStates;

namespace Content.Shared.StatusIcon.Components;

/// <summary>
/// This is used for noting if an entity is able to
/// have StatusIcons displayed on them and inherent icons. (debug purposes)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStatusIconSystem))]
public sealed partial class StatusIconComponent : Component
{
    /// <summary>
    /// Optional bounds for where the icons are laid out.
    /// If null, the sprite bounds will be used.
    /// </summary>
    [AutoNetworkedField]
    [DataField("bounds"), ViewVariables(VVAccess.ReadWrite)]
    public Box2? Bounds;
}

/// <summary>
/// Event raised directed on an entity CLIENT-SIDE ONLY
/// in order to get what status icons an entity has.
/// </summary>
/// <param name="StatusIcons"></param>
[ByRefEvent]
public record struct GetStatusIconsEvent(List<StatusIconData> StatusIcons);
