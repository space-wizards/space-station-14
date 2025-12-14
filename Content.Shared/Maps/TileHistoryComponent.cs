using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TileHistoryComponent : Component
{
    // History of tiles for each grid index. The end of the list is considered the top of the stack.
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, List<ProtoId<ContentTileDefinition>>> TileHistory = new();
}
