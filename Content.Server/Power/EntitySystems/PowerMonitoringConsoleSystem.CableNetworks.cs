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
    private void RefreshPowerCableGrid(EntityUid gridUid, MapGridComponent grid)
    {
        // Clears all chunks for the associated grid
        var allChunks = new Dictionary<Vector2i, PowerCableChunk>();
        _gridPowerCableChunks[gridUid] = allChunks;

        // Adds all power cables to the grid
        var query = AllEntityQuery<CableComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var cable, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, entXform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                allChunks[chunkOrigin] = chunk;
            }

            AddPowerCableToTile(chunk, tile.GridIndices, cable);
        }
    }

    private void AddPowerCableToTile(PowerCableChunk chunk, Vector2i tile, CableComponent cable)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
        var flag = SharedNavMapSystem.GetFlag(relative);

        chunk.PowerCableData.TryAdd(cable.CableType, 0);
        chunk.PowerCableData[cable.CableType] |= flag;
    }

    private void RemovePowerCableFromTile(PowerCableChunk chunk, Vector2i tile, CableComponent cable)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
        var flag = SharedNavMapSystem.GetFlag(relative);

        chunk.PowerCableData.TryAdd(cable.CableType, 0);
        chunk.PowerCableData[cable.CableType] &= ~flag;
    }

    private void RefreshTile(EntityUid gridUid, MapGridComponent grid, PowerCableChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
        var flag = SharedNavMapSystem.GetFlag(relative);

        foreach (var datum in chunk.PowerCableData)
            chunk.PowerCableData[datum.Key] &= ~flag;

        var enumerator = _sharedMapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (enumerator.MoveNext(out var ent))
        {
            if (TryComp<CableComponent>(ent, out var cable))
            {
                chunk.PowerCableData.TryAdd(cable.CableType, 0);
                chunk.PowerCableData[cable.CableType] |= flag;
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
