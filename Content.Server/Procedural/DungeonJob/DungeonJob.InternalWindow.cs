using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="InternalWindowDunGen"/>
    /// </summary>
    private async Task PostGen(InternalWindowDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        // Iterate every room and check if there's a gap beyond it that leads to another room within N tiles
        // If so then consider windows
        var minDistance = 4;
        var maxDistance = 6;
        var tileDef = _tileDefManager[gen.Tile];
        var contents = _prototype.Index(gen.Contents);

        foreach (var room in dungeon.Rooms)
        {
            var validTiles = new List<Vector2i>();

            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                foreach (var tile in room.Tiles)
                {
                    var tileAngle = (tile + _grid.TileSizeHalfVector - room.Center).ToAngle();
                    var roundedAngle = Math.Round(tileAngle.Theta / (Math.PI / 2)) * (Math.PI / 2);

                    var tileVec = (Vector2i) new Angle(roundedAngle).ToVec().Rounded();

                    if (!tileVec.Equals(dirVec))
                        continue;

                    var valid = false;

                    for (var j = 1; j < maxDistance; j++)
                    {
                        var edgeNeighbor = tile + dirVec * j;

                        if (dungeon.RoomTiles.Contains(edgeNeighbor))
                        {
                            if (j < minDistance)
                            {
                                valid = false;
                            }
                            else
                            {
                                valid = true;
                            }

                            break;
                        }
                    }

                    if (!valid)
                        continue;

                    var windowTile = tile + dirVec;

                    if (reservedTiles.Contains(windowTile))
                        continue;

                    if (!_anchorable.TileFree((_gridUid, _grid), windowTile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        continue;

                    validTiles.Add(windowTile);
                }

                validTiles.Sort((x, y) => (x + _grid.TileSizeHalfVector - room.Center).LengthSquared().CompareTo((y + _grid.TileSizeHalfVector - room.Center).LengthSquared()));

                for (var j = 0; j < Math.Min(validTiles.Count, 3); j++)
                {
                    var tile = validTiles[j];
                    var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile);
                    _maps.SetTile(_gridUid, _grid, tile, _tile.GetVariantTile((ContentTileDefinition) tileDef, random));

                    _entManager.SpawnEntitiesAttachedTo(gridPos, _entTable.GetSpawns(contents, random));
                }

                if (validTiles.Count > 0)
                {
                    await SuspendDungeon();

                    if (!ValidateResume())
                        return;
                }

                validTiles.Clear();
            }
        }
    }
}
