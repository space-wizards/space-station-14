using Content.Server.Atmos.Piping.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Shared.Atmos;
using Content.Shared.Nodes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Piping.EntitySystems;

/// <summary>
/// 
/// </summary>
public sealed partial class AtmosPipeNodeSystem : EntitySystem
{
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
    private EntityQuery<AtmosPipeNodeComponent> _pipeQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

        _pipeQuery = GetEntityQuery<AtmosPipeNodeComponent>();

        SubscribeLocalEvent<AtmosPipeNodeComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AtmosPipeNodeComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<AtmosPipeNodeComponent, PolyNodeRelayEvent<MoveEvent>>(OnRelayMove);
        SubscribeLocalEvent<AtmosPipeNodeComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<AtmosPipeNodeComponent, CheckEdgeEvent>(OnCheckEdge);
    }

    private IEnumerable<(EntityUid, AtmosPipeNodeComponent)> PipesInDirection(Vector2i pos, PipeDirection dir, MapGridComponent grid)
    {
        var tilePos = pos.Offset(dir.ToDirection());
        foreach (var nodeId in _nodeSystem.GetAnchoredNodesOnTile(grid, tilePos))
        {
            if (_pipeQuery.TryGetComponent(nodeId, out var pipe))
                yield return (nodeId, pipe);
        }
    }

    private IEnumerable<EntityUid> LinkableNodesInDirection(Vector2i pos, PipeDirection pipeDir, MapGridComponent grid)
    {
        foreach (var (pipeId, pipe) in PipesInDirection(pos, pipeDir, grid))
        {
            if (pipe.CurrPipeDirection.HasDirection(pipeDir.GetOpposite()))
                yield return pipeId;
        }
    }

    private void OnComponentStartup(EntityUid uid, AtmosPipeNodeComponent comp, ComponentStartup args)
    {
        if (comp.RotationEnabled)
        {
            var hostXform = Transform(_nodeSystem.GetNodeHost(uid));
            comp.CurrPipeDirection = comp.BasePipeDirection.RotatePipeDirection(hostXform.LocalRotation);
        }
        else
            comp.CurrPipeDirection = comp.BasePipeDirection;
    }

    private void OnMove(EntityUid uid, AtmosPipeNodeComponent comp, ref MoveEvent args)
    {
        if (args.NewRotation != args.OldRotation && comp.RotationEnabled)
        {
            var oldDirection = comp.CurrPipeDirection;
            comp.CurrPipeDirection = comp.BasePipeDirection.RotatePipeDirection(args.NewRotation);

            if (comp.CurrPipeDirection != oldDirection)
            {
                _nodeSystem.QueueEdgeUpdate(uid);
                return;
            }
        }

        if (args.NewPosition != args.OldPosition)
            _nodeSystem.QueueEdgeUpdate(uid);
    }

    private void OnRelayMove(EntityUid uid, AtmosPipeNodeComponent comp, ref PolyNodeRelayEvent<MoveEvent> args)
    {
        OnMove(uid, comp, ref args.Event);
    }

    /// <summary>
    /// </summary>
    private void OnUpdateEdges(EntityUid uid, AtmosPipeNodeComponent comp, ref UpdateEdgesEvent args)
    {
        var xform = args.HostXform;
        if (!xform.Anchored || args.HostGrid is not { } grid)
            return;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);
        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; ++i)
        {
            var pipeDir = (PipeDirection) (1 << i);
            if (!comp.CurrPipeDirection.HasDirection(pipeDir))
                continue;

            foreach (var pipeId in LinkableNodesInDirection(gridIndex, pipeDir, grid))
            {
                args.Edges ??= new();
                args.Edges[pipeId] = args.Edges.TryGetValue(pipeId, out var flags) ? flags | EdgeFlags.None : EdgeFlags.None;
            }
        }
    }

    /// <summary>
    /// </summary>
    private void OnCheckEdge(EntityUid uid, AtmosPipeNodeComponent comp, ref CheckEdgeEvent args)
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

        if (!comp.CurrPipeDirection.HasDirection(dir))
            return;

        if (!_pipeQuery.TryGetComponent(args.EdgeId, out var pipe))
            return;

        if (!pipe.CurrPipeDirection.HasDirection(dir.GetOpposite()))
            return;

        args.Wanted = true;
        args.Flags |= EdgeFlags.None;
    }
}
