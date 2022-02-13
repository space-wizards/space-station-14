using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed class AtmosPipeNodeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // This should probably just be a directed event, but that would require a weird component that exists only to
        // receive directed events (*cough* *cough* CableVisComponent *cough*).
        //
        // Really I want to target any entity with a PipeConnectorVisualizer or a NodeContainerComponent that contains a
        // pipe-node. But I don't know of nice way of doing that.
        SubscribeLocalEvent<NodeGroupsRebuilt>(OnNodeUpdate);
    }

    private void OnNodeUpdate(ref NodeGroupsRebuilt ev)
    {
        UpdateAppearance(ev.NodeOwner);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, NodeContainerComponent? container = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref appearance, ref container, ref xform, false))
            return;

        if (!_mapManager.TryGetGrid(xform.GridID, out var grid))
            return;

        // get connected entities
        var anyPipeNodes = false;
        HashSet<EntityUid> connected = new();
        foreach (var node in container.Nodes.Values)
        {
            if (node is not PipeNode)
                continue;

            anyPipeNodes = true;

            foreach (var connectedNode in node.ReachableNodes)
            {
                if (connectedNode is PipeNode)
                    connected.Add(connectedNode.Owner);
            }
        }

        if (!anyPipeNodes)
            return;

        // find the cardinal directions of any connected entities
        var netConnectedDirections = PipeDirection.None;
        var tile = grid.TileIndicesFor(xform.Coordinates);
        foreach (var neighbour in connected)
        {
            var otherTile = grid.TileIndicesFor(Transform(neighbour).Coordinates);

            netConnectedDirections |= (otherTile - tile) switch
            {
                (0, 1) => PipeDirection.North,
                (0, -1) => PipeDirection.South,
                (1, 0) => PipeDirection.East,
                (-1, 0) => PipeDirection.West,
                _ => PipeDirection.None
            };
        }

        appearance.SetData(PipeVisuals.VisualState, netConnectedDirections);
    }
}
