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


    private IEnumerable<(EntityUid, DirNodeComponent)> DirNodesInDirection(Vector2i pos, PipeDirection dir, MapGridComponent grid)
    {
        var tilePos = pos.Offset(dir.ToDirection());
        foreach (var nodeId in _nodeSystem.GetAnchoredNodesOnTile(grid, tilePos))
        {
            if (_dirQuery.TryGetComponent(nodeId, out var dirNode))
                yield return (nodeId, dirNode);
        }
    }

    private IEnumerable<EntityUid> LinkableNodesInDirection(Vector2i pos, PipeDirection dir, MapGridComponent grid)
    {
        foreach (var (nodeId, dirNode) in DirNodesInDirection(pos, dir, grid))
        {
            if (dirNode.CurrentDirection.HasDirection(dir.GetOpposite()))
                yield return nodeId;
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
        var xform = args.HostXform;
        if (!xform.Anchored || args.HostGrid is not { } grid)
            return;

        var pos = grid.TileIndicesFor(xform.Coordinates);
        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; ++i)
        {
            var dir = (PipeDirection) (1 << i);
            if (!comp.CurrentDirection.HasDirection(dir))
                continue;

            foreach (var nodeId in LinkableNodesInDirection(pos, dir, grid))
            {
                if (nodeId == uid)
                    continue;

                args.Edges ??= new();
                args.Edges[nodeId] = (args.Edges.TryGetValue(nodeId, out var flags) ? flags : EdgeFlags.None) | comp.Flags;
            }
        }
    }

    private void OnCheckEdge(EntityUid uid, DirNodeComponent comp, ref CheckEdgeEvent args)
    {
        var nodeXform = args.NodeHostXform;
        if (!nodeXform.Anchored || args.NodeHostGrid is not { } nodeGrid)
            return;

        var edgeXform = args.EdgeHostXform;
        if (!edgeXform.Anchored || args.EdgeHostGrid is not { } edgeGrid)
            return;

        if (!ReferenceEquals(nodeGrid, edgeGrid))
            return;

        var nodePos = nodeGrid.TileIndicesFor(nodeXform.Coordinates);
        var edgePos = edgeGrid.TileIndicesFor(edgeXform.Coordinates);
        var dir = (edgePos - nodePos) switch
        {
            (0, +1) => PipeDirection.North,
            (0, -1) => PipeDirection.South,
            (+1, 0) => PipeDirection.East,
            (-1, 0) => PipeDirection.West,
            _ => PipeDirection.None,
        };

        if (!comp.CurrentDirection.HasDirection(dir))
            return;

        if (!_dirQuery.TryGetComponent(args.EdgeId, out var dirNode))
            return;

        if (!dirNode.CurrentDirection.HasDirection(dir.GetOpposite()))
            return;

        args.Wanted = true;
        args.Flags |= EdgeFlags.None;
    }
}
