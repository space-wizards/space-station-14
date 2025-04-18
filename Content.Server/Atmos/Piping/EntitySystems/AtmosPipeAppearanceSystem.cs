using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAppearanceComponent, NodeGroupsRebuilt>(OnNodeUpdate);
    }

    private void OnNodeUpdate(EntityUid uid, PipeAppearanceComponent component, ref NodeGroupsRebuilt args)
    {
        UpdateAppearance(args.NodeOwner);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, NodeContainerComponent? container = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref appearance, ref container, ref xform, false))
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var numberOfPipeLayers = TryComp<AtmosPipeLayersComponent>(uid, out var atmosPipeLayers) ? atmosPipeLayers.NumberOfPipeLayers : 1;

        // get connected entities
        var anyPipeNodes = false;
        HashSet<(EntityUid, byte)> connected = new();

        foreach (var node in container.Nodes.Values)
        {
            if (node is not PipeNode)
                continue;

            anyPipeNodes = true;

            foreach (var connectedNode in node.ReachableNodes)
            {
                if (connectedNode is PipeNode { } pipeNode)
                    connected.Add((connectedNode.Owner, pipeNode.CurrentPipeLayer));
            }
        }

        if (!anyPipeNodes)
            return;

        // find the cardinal directions of any connected entities
        var connectedDirections = new PipeDirection[numberOfPipeLayers];
        Array.Fill(connectedDirections, PipeDirection.None);

        var tile = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        foreach (var (neighbour, pipeLayer) in connected)
        {
            if (pipeLayer >= numberOfPipeLayers)
                continue;

            var otherTile = _map.TileIndicesFor(xform.GridUid.Value, grid, Transform(neighbour).Coordinates);
            var pipeLayerDirections = connectedDirections[pipeLayer];

            pipeLayerDirections |= (otherTile - tile) switch
            {
                (0, 1) => PipeDirection.North,
                (0, -1) => PipeDirection.South,
                (1, 0) => PipeDirection.East,
                (-1, 0) => PipeDirection.West,
                _ => PipeDirection.None
            };

            connectedDirections[pipeLayer] = pipeLayerDirections;
        }

        // Convert the pipe direction array into a single int for serialization
        var netConnectedDirections = 0;

        for (var i = numberOfPipeLayers - 1; i >= 0; i--)
            netConnectedDirections += (int)connectedDirections[i] << (PipeDirectionHelpers.PipeDirections * i);

        _appearance.SetData(uid, PipeVisuals.VisualState, netConnectedDirections, appearance);
    }
}
