using Robust.Shared.GameStates;

namespace Content.Shared.Maps;

/// <summary>
///     This component goes on the grid and stores non-standard sets of tiles.
///     Standard = each tile is on its respective BaseTurf
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileStackMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, List<string>> Data = new();
}
