using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableNetworkBitMask(EntityUid uid, MapGridComponent grid)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();
        var tiles = grid.GetAllTilesEnumerator();

        while (tiles.MoveNext(out var tile))
        {
            var gridIndices = tile.Value.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, SharedNavMapSystem.ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunkPowerCables(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);
            var enumerator = grid.GetAnchoredEntitiesEnumerator(gridIndices);

            while (enumerator.MoveNext(out var ent))
            {
                if (TryComp<CableComponent>(ent, out var cable))
                {
                    if (!chunk.CableData.ContainsKey(cable.CableType))
                        chunk.CableData.Add(cable.CableType, 0);

                    chunk.CableData[cable.CableType] |= flag;
                }
            }

            // Remove empty chucks to save on bandwith
            if (chunk.CableData.Values.All(x => x == 0))
                chunks.Remove(chunkOrigin);
        }

        return chunks;
    }

    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableNetworkBitMask(EntityUid uid, MapGridComponent grid, IEnumerable<Node> nodeList)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();

        foreach (var node in nodeList)
        {
            if (node == null)
                continue;

            var ent = node.Owner;
            var xform = Transform(ent);
            var tile = _sharedMapSystem.GetTileRef(uid, grid, xform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!chunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new NavMapChunkPowerCables(chunkOrigin);
                chunks[chunkOrigin] = chunk;
            }

            var gridIndices = tile.GridIndices;
            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            if (TryComp<CableComponent>(ent, out var cable))
            {
                if (!chunk.CableData.ContainsKey(cable.CableType))
                    chunk.CableData.Add(cable.CableType, 0);

                chunk.CableData[cable.CableType] |= flag;
            }
        }

        return chunks;
    }

    // Probably faster than querying all node entities and checking their NetID?
    private List<Node> FloodFillNode(Node rootNode)
    {
        rootNode.FloodGen += 1;
        var allNodes = new List<Node>();
        var stack = new Stack<Node>();
        stack.Push(rootNode);

        while (stack.TryPop(out var node))
        {
            allNodes.Add(node);

            foreach (var reachable in node.ReachableNodes)
            {
                if (reachable.FloodGen == rootNode.FloodGen)
                    continue;

                reachable.FloodGen = rootNode.FloodGen;
                stack.Push(reachable);
            }
        }

        return allNodes;
    }
}
