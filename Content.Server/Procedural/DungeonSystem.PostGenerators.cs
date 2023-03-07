using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    /*
     * Run after the main dungeon generation
     */

    private void PostGen(BoundaryWallPostGen gen, Dungeon dungeon, Random random)
    {
        foreach (var room in dungeon.Rooms)
        {
            // Spawn wall outline
            // - Tiles first

            for (var x = -1; x <= room.Size.X; x++)
            {
                for (var y = -1; y <= room.Size.Y; y++)
                {
                    if (x != -1 && y != -1 && x != room.Size.X && y != room.Size.Y)
                    {
                        continue;
                    }

                    var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                    var tilePos = dungeonMatty.Transform((Vector2) indices + grid.TileSize / 2f - roomCenter).Floored();

                    var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(tilePos);

                    // Occupied tile.
                    if (anchoredEnts.MoveNext(out _))
                        continue;

                    tiles.Add((tilePos, new Tile(_tileDefManager["FloorSteel"].TileId)));
                }
            }

            grid.SetTiles(tiles);
            tiles.Clear();

            // Double iteration coz we bulk set tiles for speed.
            for (var x = -1; x <= room.Size.X; x++)
            {
                for (var y = -1; y <= room.Size.Y; y++)
                {
                    if (x != -1 && y != -1 && x != room.Size.X && y != room.Size.Y)
                    {
                        continue;
                    }

                    var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                    var tilePos = dungeonMatty.Transform((Vector2) indices + grid.TileSize / 2f - roomCenter).Floored();

                    var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(tilePos);

                    // Occupied tile.
                    if (anchoredEnts.MoveNext(out _))
                        continue;

                    Spawn("WallSolid", grid.GridTileToLocal(tilePos));
                }
            }
        }
    }

    private void PostGen(EntrancePostGen gen, Dungeon dungeon, Random random)
    {
        // TODO:
    }

    private void PostGen(PoweredAirlockPostGen gen, Dungeon dungeon, Random random)
    {
        // TODO: Need a test that none of the rooms touch each other.

        // Grab all of the room bounds
        // Then, work out connections between them
        // TODO: Could use arrays given we do know room count up front
        var rooms = new ValueList<Box2i>(chosenPacks.Length);
        var roomBorders = new Dictionary<Box2i, HashSet<Vector2i>>(chosenPacks.Length);

        for (var i = 0; i < chosenPacks.Length; i++)
        {
            var pack = chosenPacks[i];
            var transform = packTransforms[i];

            foreach (var room in pack!.Rooms)
            {
                // Rooms are at 0,0, need them offset from center
                var offRoom = ((Box2) room).Translated(-pack.Size / 2f);

                var dRoom = (Box2i) transform.TransformBox(offRoom);
                rooms.Add(dRoom);
                DebugTools.Assert(dRoom.Size.X * dRoom.Size.Y == room.Size.X * room.Size.Y);
                var roomEdges = new HashSet<Vector2i>();
                var rator = new Box2iEdgeEnumerator(dRoom, false);

                while (rator.MoveNext(out var edge))
                {
                    roomEdges.Add(edge);
                }

                roomBorders.Add(dRoom, roomEdges);
            }
        }

        // Do pathfind from first room to work out graph.
        // TODO: Optional loops

        var roomConnections = new Dictionary<Box2i, List<Box2i>>();
        var frontier = new Queue<Box2i>();
        frontier.Enqueue(rooms[0]);

        while (frontier.TryDequeue(out var room))
        {
            var conns = roomConnections.GetOrNew(room);
            var border = roomBorders[room];

            foreach (var (otherRoom, otherBorders) in roomBorders)
            {
                if (room.Equals(otherRoom) ||
                    conns.Contains(otherRoom))
                {
                    continue;
                }

                var flipp = new HashSet<Vector2i>(border);
                flipp.IntersectWith(otherBorders);

                if (flipp.Count == 0)
                    continue;

                // Spawn the edge airlocks
                // Weight towards center of the group but not always.

                // If there's 3 overlaps just do a 3x1
                if (flipp.Count == 3)
                {
                    foreach (var node in flipp)
                    {
                        var dungeonNode = dungeonTransform.Transform((Vector2) node + grid.TileSize / 2f).Floored();
                        grid.SetTile(dungeonNode, new Tile(_tileDefManager["FloorSteel"].TileId));
                        Spawn("AirlockGlass", grid.GridTileToLocal(dungeonNode));
                    }
                }
                else
                {
                    // Pick a random one weighted towards the center
                    var center = Vector2.Zero;

                    foreach (var node in flipp)
                    {
                        center += (Vector2) node + grid.TileSize / 2f;
                    }

                    center /= flipp.Count;
                    // Weight airlocks towards center more.
                    var nodeDistances = new List<(Vector2i Node, float Distance)>(flipp.Count);

                    foreach (var node in flipp)
                    {
                        nodeDistances.Add((node, ((Vector2) node + grid.TileSize / 2f - center).LengthSquared));
                    }

                    nodeDistances.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                    var width = 1;

                    for (var i = 0; i < nodeDistances.Count; i++)
                    {
                        width--;
                        var node = nodeDistances[i].Node;
                        var adjustedNode = dungeonTransform.Transform((Vector2) node + grid.TileSize / 2f).Floored();
                        grid.SetTile(adjustedNode, new Tile(_tileDefManager["FloorSteel"].TileId));
                        Spawn("AirlockGlass", grid.GridTileToLocal(adjustedNode));

                        if (width == 0)
                            break;
                    }
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
                frontier.Enqueue(otherRoom);
            }
        }
    }
}
