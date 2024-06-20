using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="SplineDungeonConnectorPostGen"/>
    /// </summary>
    private async Task PostGen(
        SplineDungeonConnectorPostGen gen,
        DungeonData data,
        List<Dungeon> dungeons,
        HashSet<Vector2i> reservedTiles,
        Random random)
    {
        // TODO: The path itself use the tile
        // Widen it randomly (probably for each tile offset it by some changing amount).

        // NOOP
        if (dungeons.Count <= 1)
            return;

        var nodes = new List<Vector2i>();

        foreach (var dungeon in dungeons)
        {
            foreach (var room in dungeon.Rooms)
            {
                if (room.Entrances.Count == 0)
                    continue;

                nodes.Add(room.Entrances[0]);
                break;
            }
        }

        var tree = _dungeon.MinimumSpanningTree(nodes, random);
        await SuspendDungeon();

        if (!ValidateResume())
            return;

        var tiles = new List<(Vector2i Index, Tile Tile)>();

        foreach (var pair in tree)
        {
            var path = _entManager.System<PathfindingSystem>().GetSplinePath(new PathfindingSystem.SplinePathArgs()
            {
                Args = new PathfindingSystem.PathArgs()
                {
                    Start = pair.Start,
                    End = pair.End,
                }
            }, random);

            await SuspendDungeon();

            if (!ValidateResume())
                return;

            foreach (var node in path.Path)
            {
                if (reservedTiles.Contains(node))
                    continue;

                tiles.Add((node, new Tile(_prototype.Index(new ProtoId<ContentTileDefinition>("FloorSteel")).TileId)));
            }
        }

        _maps.SetTiles(_gridUid, _grid, tiles);
    }
}
