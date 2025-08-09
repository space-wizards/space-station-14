using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

[RegisterComponent, NetworkedComponent]
public sealed partial class TileHistoryComponent : Component
{
    [DataField]
    public Dictionary<Vector2i, Stack<ProtoId<ContentTileDefinition>>> TileHistory = new();
}
