using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
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

        _appearance.SetData(uid, PipeVisuals.VisualState, netConnectedDirections, appearance);
    }
}
