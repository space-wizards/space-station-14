using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="WallMountDunGen"/>
    /// </summary>
    private async Task PostGen(WallMountDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var checkedTiles = new HashSet<Vector2i>();
        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var tileDef = (ContentTileDefinition) _tileDefManager[gen.Tile];
        var contents = _prototype.Index(gen.Contents);

        foreach (var neighbor in allExterior)
        {
            // Occupado
            if (dungeon.RoomTiles.Contains(neighbor) || checkedTiles.Contains(neighbor) || !_anchorable.TileFree((_gridUid, _grid), neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            if (!random.Prob(gen.Prob) || !checkedTiles.Add(neighbor))
                continue;

            if (reservedTiles.Contains(neighbor))
                continue;

            var tileVariant = _tile.GetVariantTile(tileDef, random);
            _maps.SetTile(_gridUid, _grid, neighbor, tileVariant);
            AddLoadedTile(neighbor, tileVariant);
            var gridPos = _maps.GridTileToLocal(_gridUid, _grid, neighbor);
            var protoNames = _entTable.GetSpawns(contents, random);

            var uids = _entManager.SpawnEntitiesAttachedTo(gridPos, protoNames);

            foreach (var uid in uids)
            {
                AddLoadedEntity(neighbor, uid);
            }

            await SuspendDungeon();
            if (!ValidateResume())
                return;
        }
    }
}
