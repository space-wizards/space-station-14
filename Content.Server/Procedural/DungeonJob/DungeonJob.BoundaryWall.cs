using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="BoundaryWallDunGen"/>
    /// </summary>
    private async Task PostGen(BoundaryWallDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var tileDef = _tileDefManager[gen.Tile];
        var tiles = new List<(Vector2i Index, Tile Tile)>(dungeon.RoomExteriorTiles.Count);

        var wall = gen.Wall;
        var cornerWall = gen.CornerWall ?? gen.Wall;

        // Spawn wall outline
        // - Tiles first
        foreach (var neighbor in dungeon.RoomExteriorTiles)
        {
            DebugTools.Assert(!dungeon.RoomTiles.Contains(neighbor));

            if (dungeon.Entrances.Contains(neighbor))
                continue;

            if (!_anchorable.TileFree(_grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            tiles.Add((neighbor, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
        }

        foreach (var index in dungeon.CorridorExteriorTiles)
        {
            if (dungeon.RoomTiles.Contains(index))
                continue;

            if (!_anchorable.TileFree(_grid, index, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            tiles.Add((index, _tile.GetVariantTile((ContentTileDefinition)tileDef, random)));
        }

        _maps.SetTiles(_gridUid, _grid, tiles);

        // Double iteration coz we bulk set tiles for speed.
        for (var i = 0; i < tiles.Count; i++)
        {
            var index = tiles[i];

            if (!_anchorable.TileFree(_grid, index.Index, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            // If no cardinal neighbors in dungeon then we're a corner.
            var isCorner = true;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0)
                    {
                        continue;
                    }

                    var neighbor = new Vector2i(index.Index.X + x, index.Index.Y + y);

                    if (dungeon.RoomTiles.Contains(neighbor) || dungeon.CorridorTiles.Contains(neighbor))
                    {
                        isCorner = false;
                        break;
                    }
                }

                if (!isCorner)
                    break;
            }

            if (isCorner)
                _entManager.SpawnEntity(cornerWall, _maps.GridTileToLocal(_gridUid, _grid, index.Index));

            if (!isCorner)
                _entManager.SpawnEntity(wall, _maps.GridTileToLocal(_gridUid, _grid, index.Index));

            if (i % 20 == 0)
            {
                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }
    }
}
