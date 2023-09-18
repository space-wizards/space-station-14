using Content.Server.Ame.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;

namespace Content.Server.Ame.EntitySystems;

public sealed partial class AmeNodeSystem : EntitySystem
{
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeNodeComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<AmeNodeComponent, CheckEdgeEvent>(OnCheckEdge);
    }


    private void OnUpdateEdges(EntityUid uid, AmeNodeComponent comp, ref UpdateEdgesEvent args)
    {
        var xform = args.HostXform;
        if (!xform.Anchored || args.HostGrid is not { } grid)
            return;

        // Foreach in cardinals.
        var gridIndex = grid.TileIndicesFor(xform.Coordinates);
        foreach (var (nodeId, _) in _nodeSystem.GetAnchoredNodesInDir(grid, gridIndex, Direction.North, Direction.South, Direction.East, Direction.West))
        {
            if (!HasComp<AmeNodeComponent>(nodeId) | nodeId == uid)
                continue;

            args.Edges ??= new();

            if (!args.Edges.TryGetValue(nodeId, out var oldFlags))
                oldFlags = EdgeFlags.None;

            args.Edges[nodeId] = oldFlags | EdgeFlags.None;
        }

        foreach (var (nodeId, _) in _nodeSystem.GetAnchoredNodesInDir(grid, gridIndex, Direction.NorthWest, Direction.SouthWest, Direction.NorthEast, Direction.SouthEast))
        {
            if (!HasComp<AmeNodeComponent>(nodeId) | nodeId == uid)
                continue;

            args.Edges ??= new();

            if (!args.Edges.TryGetValue(nodeId, out var oldFlags))
                oldFlags = EdgeFlags.None;

            args.Edges[nodeId] = oldFlags | EdgeFlags.NoMerge;
        }
    }

    private void OnCheckEdge(EntityUid uid, AmeNodeComponent comp, ref CheckEdgeEvent args)
    {
        var nodeXform = args.NodeHostXform;
        if (!nodeXform.Anchored || args.NodeHostGrid is not { } nodeGrid)
            return;

        var edgeXform = args.EdgeHostXform;
        if (!edgeXform.Anchored || args.EdgeHostGrid is not { } edgeGrid)
            return;

        if (nodeGrid != edgeGrid)
            return;

        var delta = edgeGrid.TileIndicesFor(edgeXform.Coordinates) - nodeGrid.TileIndicesFor(nodeXform.Coordinates);
        switch (delta)
        {
            case (0, +1):
            case (0, -1):
            case (+1, 0):
            case (-1, 0):
                break;
            case (+1, +1):
            case (+1, -1):
            case (-1, +1):
            case (-1, -1):
                args.Flags |= EdgeFlags.NoMerge;
                break;
            default:
                return;
        }

        args.Wanted = true;
    }
}
