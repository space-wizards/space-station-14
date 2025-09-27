using Robust.Shared.Map;

namespace Content.Shared.Random.Rules.TileRules;

/// <summary>
/// Rule for whether a specific tile is just empty space.
/// </summary>
public sealed partial class OnEmptyTileRule : TileRule
{
    public override bool Check(EntityManager entManager, EntityUid tileParentUid, TileRef tile, Vector2i position, HashSet<EntityUid> intersectingEntities)
        => tile.Tile.IsEmpty ^ Inverted;
}
