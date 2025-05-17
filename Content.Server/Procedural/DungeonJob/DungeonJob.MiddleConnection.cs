using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="MiddleConnectionDunGen"/>
    /// </summary>
    private async Task PostGen(MiddleConnectionDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        // Grab all of the room bounds
        // Then, work out connections between them
        var roomBorders = new Dictionary<DungeonRoom, HashSet<Vector2i>>(dungeon.Rooms.Count);

        foreach (var room in dungeon.Rooms)
        {
            var roomEdges = new HashSet<Vector2i>();

            foreach (var index in room.Tiles)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        // Cardinals only
                        if (x != 0 && y != 0 ||
                            x == 0 && y == 0)
                        {
                            continue;
                        }

                        var neighbor = new Vector2i(index.X + x, index.Y + y);

                        if (dungeon.RoomTiles.Contains(neighbor))
                            continue;

                        if (!_anchorable.TileFree(_grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                            continue;

                        roomEdges.Add(neighbor);
                    }
                }
            }

            roomBorders.Add(room, roomEdges);
        }

        // Do pathfind from first room to work out graph.
        // TODO: Optional loops

        var roomConnections = new Dictionary<DungeonRoom, List<DungeonRoom>>();
        var tileDef = _tileDefManager[gen.Tile];
        _prototype.TryIndex(gen.Flank, out var flankContents);
        var contents = _prototype.Index(gen.Contents);

        foreach (var (room, border) in roomBorders)
        {
            var conns = roomConnections.GetOrNew(room);

            foreach (var (otherRoom, otherBorders) in roomBorders)
            {
                if (room.Equals(otherRoom) ||
                    conns.Contains(otherRoom))
                {
                    continue;
                }

                var flipp = new HashSet<Vector2i>(border);
                flipp.IntersectWith(otherBorders);

                if (flipp.Count == 0 ||
                    gen.OverlapCount != -1 && flipp.Count != gen.OverlapCount)
                    continue;

                var center = Vector2.Zero;

                foreach (var node in flipp)
                {
                    center += node + _grid.TileSizeHalfVector;
                }

                center /= flipp.Count;
                // Weight airlocks towards center more.
                var nodeDistances = new List<(Vector2i Node, float Distance)>(flipp.Count);

                foreach (var node in flipp)
                {
                    nodeDistances.Add((node, (node + _grid.TileSizeHalfVector - center).LengthSquared()));
                }

                nodeDistances.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                var width = gen.Count;

                for (var i = 0; i < nodeDistances.Count; i++)
                {
                    var node = nodeDistances[i].Node;
                    var gridPos = _maps.GridTileToLocal(_gridUid, _grid, node);
                    if (!_anchorable.TileFree(_grid, node, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        continue;

                    width--;
                    _maps.SetTile(_gridUid, _grid, node, _tile.GetVariantTile((ContentTileDefinition) tileDef, random));

                    if (flankContents != null && nodeDistances.Count - i <= 2)
                    {
                        _entManager.SpawnEntitiesAttachedTo(gridPos, _entTable.GetSpawns(flankContents, random));
                    }
                    else
                    {
                        // Iterate neighbors and check for blockers, if so bulldoze
                        ClearDoor(dungeon, _grid, node);

                        _entManager.SpawnEntitiesAttachedTo(gridPos, _entTable.GetSpawns(contents, random));
                    }

                    if (width == 0)
                        break;
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }
    }
}
