using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private void RefreshGrid(EntityUid uid, PowerMonitoringConsoleComponent component, EntityUid gridUid, MapGridComponent grid)
    {
        component.AllChunks.Clear();

        var tiles = _sharedMapSystem.GetAllTilesEnumerator(gridUid, grid);
        while (tiles.MoveNext(out var tile))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.Value.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!component.AllChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                component.AllChunks[chunkOrigin] = chunk;
            }

            RefreshTile(uid, component, gridUid, grid, chunk, tile.Value.GridIndices);
        }
    }

    private void RefreshTile
        (EntityUid uid,
        PowerMonitoringConsoleComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        PowerCableChunk chunk,
        Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
        var existing = chunk.PowerCableData.ShallowClone();
        var flag = SharedNavMapSystem.GetFlag(relative);

        foreach (var datum in chunk.PowerCableData)
            chunk.PowerCableData[datum.Key] &= ~flag;

        var enumerator = _sharedMapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (TryComp<CableComponent>(ent, out var cable))
            {
                if (!chunk.PowerCableData.ContainsKey(cable.CableType))
                    chunk.PowerCableData.Add(cable.CableType, 0);

                chunk.PowerCableData[cable.CableType] |= flag;
            }
        }

        if (chunk.PowerCableData.All(x => x.Value == 0))
            component.AllChunks.Remove(chunk.Origin);

        foreach (var datum in chunk.PowerCableData)
        {
            if (!existing.ContainsKey(datum.Key) || existing[datum.Key] != datum.Value)
            {
                Dirty(uid, component);
                return;
            }
        }
    }

    private void UpdateFocusNetwork(EntityUid uid, PowerMonitoringConsoleComponent component, EntityUid gridUid, MapGridComponent grid, IEnumerable<EntityUid> nodeList)
    {
        component.FocusChunks.Clear();

        foreach (var ent in nodeList)
        {
            var xform = Transform(ent);
            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, xform.Coordinates);
            var gridIndices = tile.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, SharedNavMapSystem.ChunkSize);

            if (!component.FocusChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                component.FocusChunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            if (TryComp<CableComponent>(ent, out var cable))
            {
                if (!chunk.PowerCableData.ContainsKey(cable.CableType))
                    chunk.PowerCableData.Add(cable.CableType, 0);

                chunk.PowerCableData[cable.CableType] |= flag;
            }
        }

        Dirty(uid, component);
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
