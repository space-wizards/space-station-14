using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Shared.Random.Rules.TileRules;

/// <summary>
/// Rule for whether a specific tile is an exposed (sub)floor (e.g. under-tile plating or lattice, but not on space itself).
/// </summary>
public sealed partial class OnExposedSubfloorTileRule : TileRule
{
    public override bool Check(EntityManager entManager, EntityUid tileParentUid, TileRef tile, Vector2i position, HashSet<EntityUid> intersectingEntities)
        => tile.Tile.GetContentTileDefinition().IsSubFloor ^ Inverted;
}
