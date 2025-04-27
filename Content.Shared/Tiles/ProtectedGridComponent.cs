using Robust.Shared.GameStates;

namespace Content.Shared.Tiles;

/// <summary>
/// Prevents floor tile updates when attached to a grid.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ProtectedGridSystem))]
public sealed partial class ProtectedGridComponent : Component
{
    /// <summary>
    /// A hashset of all the initial tiles of this grid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Vector2i> BaseIndices = new();
}
