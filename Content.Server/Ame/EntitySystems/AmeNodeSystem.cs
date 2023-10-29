using Content.Server.Ame.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;

namespace Content.Server.Ame.EntitySystems;

public sealed partial class AmeNodeSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeNodeComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<AmeNodeComponent, CheckEdgeEvent>(OnCheckEdge);
    }


    private void OnUpdateEdges(EntityUid uid, AmeNodeComponent comp, ref UpdateEdgesEvent args)
    {
        if (args.Host.Comp is not { Anchored: true } xform || args.Grid is not { } grid)
            return;

        // Foreach in cardinals.
        var gridIndex = _mapSystem.TileIndicesFor(grid.Owner, grid.Comp, xform.Coordinates);
        foreach (var (node, _) in _nodeSystem.GetAnchoredNodesInDirs(grid, gridIndex, Direction.North, Direction.South, Direction.East, Direction.West))
        {
            if (node.Owner == uid || !HasComp<AmeNodeComponent>(node))
                continue;

            args.Edges ??= new();
            args.Edges[node] = (args.Edges.TryGetValue(node, out var oldFlags) ? oldFlags : EdgeFlags.None) | EdgeFlags.None;
        }

        foreach (var (node, _) in _nodeSystem.GetAnchoredNodesInDirs(grid, gridIndex, Direction.NorthWest, Direction.SouthWest, Direction.NorthEast, Direction.SouthEast))
        {
            if (node.Owner == uid || !HasComp<AmeNodeComponent>(node))
                continue;

            args.Edges ??= new();
            args.Edges[node] = (args.Edges.TryGetValue(node, out var oldFlags) ? oldFlags : EdgeFlags.None) | EdgeFlags.NoMerge;
        }
    }

    private void OnCheckEdge(EntityUid uid, AmeNodeComponent comp, ref CheckEdgeEvent args)
    {
        if (args.FromHost.Comp is not { Anchored: true } nodeXform || args.FromGrid is not { } nodeGrid)
            return;

        if (args.ToHost.Comp is not { Anchored: true } edgeXform || args.ToGrid is not { } edgeGrid)
            return;

        if (nodeGrid != edgeGrid)
            return;

        var delta = _mapSystem.TileIndicesFor(edgeGrid.Owner, edgeGrid.Comp, edgeXform.Coordinates) - _mapSystem.TileIndicesFor(nodeGrid.Owner, nodeGrid.Comp, nodeXform.Coordinates);
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
