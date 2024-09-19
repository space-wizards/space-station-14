using System.Linq;
using System.Threading.Tasks;
using Content.Server.NodeContainer;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="AutoCablingDunGen"/>
    /// </summary>
    private async Task PostGen(AutoCablingDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Entities.TryGetValue(DungeonDataKey.Cabling, out var ent))
        {
            LogDataError(typeof(AutoCablingDunGen));
            return;
        }

        // There's a lot of ways you could do this.
        // For now we'll just connect every LV cable in the dungeon.
        var cableTiles = new HashSet<Vector2i>();
        var allTiles = new HashSet<Vector2i>(dungeon.CorridorTiles);
        allTiles.UnionWith(dungeon.RoomTiles);
        allTiles.UnionWith(dungeon.RoomExteriorTiles);
        allTiles.UnionWith(dungeon.CorridorExteriorTiles);
        var nodeQuery = _entManager.GetEntityQuery<NodeContainerComponent>();

        // Gather existing nodes
        foreach (var tile in allTiles)
        {
            var anchored = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, tile);

            while (anchored.MoveNext(out var anc))
            {
                if (!nodeQuery.TryGetComponent(anc, out var nodeContainer) ||
                   !nodeContainer.Nodes.ContainsKey("power"))
                {
                    continue;
                }

                cableTiles.Add(tile);
                break;
            }
        }

        // Iterating them all might be expensive.
        await SuspendDungeon();

        if (!ValidateResume())
            return;

        var startNodes = new List<Vector2i>(cableTiles);
        random.Shuffle(startNodes);
        var start = startNodes[0];
        var remaining = new HashSet<Vector2i>(startNodes);
        var frontier = new PriorityQueue<Vector2i, float>();
        frontier.Enqueue(start, 0f);
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var costSoFar = new Dictionary<Vector2i, float>();
        var lastDirection = new Dictionary<Vector2i, Direction>();
        costSoFar[start] = 0f;
        lastDirection[start] = Direction.Invalid;

        while (remaining.Count > 0)
        {
            if (frontier.Count == 0)
            {
                var newStart = remaining.First();
                frontier.Enqueue(newStart, 0f);
                lastDirection[newStart] = Direction.Invalid;
            }

            var node = frontier.Dequeue();

            if (remaining.Remove(node))
            {
                var weh = node;

                while (cameFrom.TryGetValue(weh, out var receiver))
                {
                    cableTiles.Add(weh);
                    weh = receiver;

                    if (weh == start)
                        break;
                }
            }

            if (!_maps.TryGetTileRef(_gridUid, _grid, node, out var tileRef) || tileRef.Tile.IsEmpty)
            {
                continue;
            }

            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction) (i * 2);

                var neighbor = node + dir.ToIntVec();
                var tileCost = 1f;

                // Prefer straight lines.
                if (lastDirection[node] != dir)
                {
                    tileCost *= 1.1f;
                }

                if (cableTiles.Contains(neighbor))
                {
                    tileCost *= 0.1f;
                }

                // Prefer tiles without walls on them
                if (HasWall(neighbor))
                {
                    tileCost *= 20f;
                }

                var gScore = costSoFar[node] + tileCost;

                if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                cameFrom[neighbor] = node;
                costSoFar[neighbor] = gScore;
                lastDirection[neighbor] = dir;
                frontier.Enqueue(neighbor, gScore);
            }
        }

        foreach (var tile in cableTiles)
        {
            if (reservedTiles.Contains(tile))
                continue;

            var anchored = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, tile);
            var found = false;

            while (anchored.MoveNext(out var anc))
            {
                if (!nodeQuery.TryGetComponent(anc, out var nodeContainer) ||
                    !nodeContainer.Nodes.ContainsKey("power"))
                {
                    continue;
                }

                found = true;
                break;
            }

            if (found)
                continue;

            _entManager.SpawnEntity(ent, _maps.GridTileToLocal(_gridUid, _grid, tile));
        }
    }
}
