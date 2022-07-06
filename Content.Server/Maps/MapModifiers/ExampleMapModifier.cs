using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Maps.MapModifiers;

[DataDefinition, PublicAPI]
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
        if (grids.Count > 0)
        {
            Logger.Debug("At least one grid on map");
            foreach (var gridUid in grids)
            {
                var id = _tileMg[TileId].TileId;
                var gridEntity = _mapMg.GetGrid(gridUid);
                gridEntity.SetTile(new Vector2i((int)TilePos.X, (int)TilePos.Y), new Tile(id));
            }
        }
    }
}
