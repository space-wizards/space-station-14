using Content.Server.Nodes.Components;
using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.Events;
using Content.Shared.Atmos;
using Content.Shared.Nodes;
using Content.Shared.Tag;
using Robust.Shared.Map.Components;

namespace Content.Server.Nodes.EntitySystems.Autolinkers;

/// <summary>
/// A graph node autoconnector component that forms connections between anchored nodes on adjacent tiles in specific directions.
/// Behaviour is handled by <see cref="PortNodeSystem"/>
/// </summary>
public sealed partial class DirNodeSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private EntityQuery<DirNodeComponent> _dirQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _dirQuery = GetEntityQuery<DirNodeComponent>();

        SubscribeLocalEvent<DirNodeComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DirNodeComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<DirNodeComponent, PolyNodeRelayEvent<MoveEvent>>(OnRelayMove);
        SubscribeLocalEvent<DirNodeComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<DirNodeComponent, CheckEdgeEvent>(OnCheckEdge);
    }


    private IEnumerable<Entity<GraphNodeComponent, DirNodeComponent>> DirNodesInDirection(Entity<MapGridComponent> grid, Vector2i pos, PipeDirection dir)
    {
        foreach (var (nodeId, node) in _nodeSystem.GetAnchoredNodesInDir(grid, pos, dir.ToDirection()))
        {
            if (_dirQuery.TryGetComponent(nodeId, out var dirNode))
                yield return (nodeId, node, dirNode);
        }
    }

    private IEnumerable<Entity<GraphNodeComponent, DirNodeComponent>> LinkableNodesInDirection(Entity<MapGridComponent> grid, Vector2i pos, PipeDirection dir)
    {
        var opposite = dir.GetOpposite();
        foreach (var (nodeId, node, dirNode) in DirNodesInDirection(grid, pos, dir))
        {
            if (dirNode.CurrentDirection.HasDirection(opposite))
                yield return (nodeId, node, dirNode);
        }
    }


    private void OnComponentStartup(EntityUid uid, DirNodeComponent comp, ComponentStartup args)
    {
        if (comp.RotationEnabled)
        {
            var hostXform = Transform(_nodeSystem.GetNodeHost(uid));
            comp.CurrentDirection = comp.BaseDirection.RotatePipeDirection(hostXform.LocalRotation);
        }
        else
            comp.CurrentDirection = comp.BaseDirection;
    }

    private void OnMove(EntityUid uid, DirNodeComponent comp, ref MoveEvent args)
    {
        if (args.NewRotation != args.OldRotation && comp.RotationEnabled)
        {
            var oldDirection = comp.CurrentDirection;
            comp.CurrentDirection = comp.BaseDirection.RotatePipeDirection(args.NewRotation);

            if (comp.CurrentDirection != oldDirection)
            {
                _nodeSystem.QueueEdgeUpdate(uid);
                return;
            }
        }

        if (args.NewPosition != args.OldPosition)
            _nodeSystem.QueueEdgeUpdate(uid);
    }

    private void OnRelayMove(EntityUid uid, DirNodeComponent comp, ref PolyNodeRelayEvent<MoveEvent> args)
    {
        OnMove(uid, comp, ref args.Event);
    }

    private void OnUpdateEdges(EntityUid uid, DirNodeComponent comp, ref UpdateEdgesEvent args)
    {
        if (args.Host.Comp is not { Anchored: true } xform || args.Grid is not { } grid)
            return;

        var pos = _mapSystem.TileIndicesFor(grid.Owner, grid.Comp, xform.Coordinates);
        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; ++i)
        {
            var dir = (PipeDirection) (1 << i);
            if (!comp.CurrentDirection.HasDirection(dir))
                continue;

            foreach (var node in LinkableNodesInDirection(grid, pos, dir))
            {
                if (node.Owner == uid)
                    continue;

                args.Edges ??= new();
                args.Edges[node] = (args.Edges.TryGetValue(node, out var flags) ? flags : EdgeFlags.None) | comp.Flags;
            }
        }
    }

    private void OnCheckEdge(EntityUid uid, DirNodeComponent comp, ref CheckEdgeEvent args)
    {
        if (args.FromHost.Comp is not { Anchored: true } nodeXform || args.FromGrid is not { } nodeGrid)
            return;

        if (args.ToHost.Comp is not { Anchored: true } edgeXform || args.ToGrid is not { } edgeGrid)
            return;

        if (nodeGrid != edgeGrid)
            return;

        var nodePos = _mapSystem.TileIndicesFor(nodeGrid.Owner, nodeGrid.Comp, nodeXform.Coordinates);
        var edgePos = _mapSystem.TileIndicesFor(edgeGrid.Owner, edgeGrid.Comp, edgeXform.Coordinates);
        var dir = (edgePos - nodePos) switch
        {
            (0, +1) => PipeDirection.North,
            (0, -1) => PipeDirection.South,
            (+1, 0) => PipeDirection.East,
            (-1, 0) => PipeDirection.West,
            _ => PipeDirection.None,
        };

        if (dir == PipeDirection.None)
            return;

        if (!comp.CurrentDirection.HasDirection(dir))
            return;

        if (!_dirQuery.TryGetComponent(args.To, out var toDir))
            return;

        if (!toDir.CurrentDirection.HasDirection(dir.GetOpposite()))
            return;

        args.Wanted = true;
        args.Flags |= EdgeFlags.None;
    }
}
