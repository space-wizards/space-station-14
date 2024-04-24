using Content.Server.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for a game rule that loads a map when activated.
/// </summary>
[RegisterComponent]
public sealed partial class LoadMapRuleComponent : Component
{
    [DataField]
    public MapId? Map;

    [DataField]
    public ProtoId<GameMapPrototype>? GameMap ;

    [DataField]
    public ResPath? MapPath;

    [DataField]
    public List<EntityUid> MapGrids = new();

    [DataField]
    public EntityWhitelist? SpawnerWhitelist;
}
