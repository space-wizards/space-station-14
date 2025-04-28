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
    /// A bitmask of all the initial tiles on this grid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, ulong> BaseIndices = new();
}
