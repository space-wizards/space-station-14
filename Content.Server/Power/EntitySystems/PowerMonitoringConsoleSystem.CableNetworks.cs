using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableNetworkBitMask(EntityUid gridUid, MapGridComponent grid)
    {
        var nodeList = new List<Node>();
        var query = AllEntityQuery<NodeContainerComponent, CableComponent>();

        while (query.MoveNext(out var ent, out var nodeContainer, out var _))
        {
            if (Transform(ent).GridUid != gridUid)
                continue;

            var node = nodeContainer.Nodes.FirstOrNull()?.Value;

            if (node == null)
                continue;

            nodeList.Add(node);
        }

        return GetPowerCableNetworkBitMask(gridUid, grid, nodeList);
    }

    private Dictionary<Vector2i, NavMapChunkPowerCables> GetPowerCableNetworkBitMask(EntityUid gridUid, MapGridComponent grid, IEnumerable<Node> nodeList)
    {
        var chunks = new Dictionary<Vector2i, NavMapChunkPowerCables>();

        foreach (var node in nodeList)
        {
            if (node == null)
                continue;

            var ent = node.Owner;
            var xform = Transform(ent);
            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, xform.Coordinates);
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
