using Robust.Shared.GameStates;

namespace Content.Shared.Wall;

/// <summary>
/// This component is placed on a wall that has entities parented to it.
/// When the wall is destroyed, it will delete the entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ParentToWallSystem))]
public sealed partial class ParentedWallComponent : Component
{
    /// <summary>
    /// The entities parented to this wall.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Children = new();
}
