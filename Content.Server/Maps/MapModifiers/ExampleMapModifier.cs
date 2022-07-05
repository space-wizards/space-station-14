using Robust.Shared.Map;

namespace Content.Server.Maps.MapModifiers;

public sealed class ExampleMapModifier : MapModifier
{
    [DataField("tileId")]
    public string TileId = "floor_wood";

    [DataField("tilePos")]
    public Vector2 TilePos = default!;

    [Dependency] private readonly IEntityManager _entityMg = default!;
    [Dependency] private readonly IMapManager _mapMg = default!;
    [Dependency] private readonly ITileDefinitionManager _tileMg = default!;

    public override void Execute(MapId mapId, IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> grids)
    {
        IMapGrid gridEntity;
        if (grids.Count == 0)
        {
            Logger.Debug("No grid found on map, adding a new one");
            gridEntity = _mapMg.CreateGrid(mapId);
        }
        else
        {
            Logger.Debug("At least one grid on map");
            gridEntity = _mapMg.GetGrid(grids[0]);
        }

        var id = _tileMg[TileId].TileId;
        gridEntity.SetTile(new Vector2i((int)TilePos.X, (int)TilePos.Y), new Tile(id));
    }
}
