using Content.Server.Nodes.Components;
using Content.Server.Nodes.Components.Debugging;
using Content.Server.Nodes.Events;

namespace Content.Server.Nodes.EntitySystems.Debugging;

/// <summary>
/// The system that handles the behaviour of the debug autolinker.
/// </summary>
public sealed partial class DebugNodeAutolinkerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookupSys = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    private EntityQuery<GraphNodeComponent> _nodeQuery = default!;
    private EntityQuery<TransformComponent> _xformQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _nodeQuery = GetEntityQuery<GraphNodeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<DebugNodeAutolinkerComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<DebugNodeAutolinkerComponent, CheckEdgeEvent>(OnCheckEdge);
    }


    /// <summary>
    /// Links any nodes with the debug autolinker to any other compatible nodes within range.
    /// </summary>
    private void OnUpdateEdges(EntityUid uid, DebugNodeAutolinkerComponent comp, ref UpdateEdgesEvent args)
    {
        if (!_xformQuery.TryGetComponent(args.Host, out var xform))
            return;

        var epicenter = _xformSys.GetWorldPosition(xform);
        var range = comp.BaseRange * comp.BaseRange;

        foreach (var nearId in _lookupSys.GetEntitiesInRange(xform.Coordinates, comp.BaseRange))
        {
            if (nearId == uid)
                continue;
            if (!_nodeQuery.TryGetComponent(nearId, out var edge))
                continue;
            if (!comp.AllowBridging && edge.GraphProto != args.Node.Comp.GraphProto)
                continue;

            var distance = (epicenter - _xformSys.GetWorldPosition(nearId)).LengthSquared();
            if (distance > range)
                continue;

            args.Edges ??= new();
            args.Edges.Add(nearId, comp.Flags);
        }
    }

    /// <summary>
    /// Maintains edges between nodes with the debug autolinker and other compatible nodes until they move out of range.
    /// </summary>
    private void OnCheckEdge(EntityUid uid, DebugNodeAutolinkerComponent comp, ref CheckEdgeEvent args)
    {
        if (args.Wanted)
            return;

        if (!comp.AllowBridging && args.To.Comp.GraphProto != args.From.Comp.GraphProto)
            return;

        if (!_xformQuery.TryGetComponent(args.FromHost, out var xform))
            return;
        if (!_xformQuery.TryGetComponent(args.ToHost, out var edgeXform))
            return;

        var range = comp.BaseRange + MathF.Max(comp.HysteresisRange, 0f);
        var distance = (_xformSys.GetWorldPosition(xform) - _xformSys.GetWorldPosition(edgeXform)).LengthSquared();
        if (distance > range * range)
            return;

        args.Flags |= comp.Flags;
        args.Wanted = true;
    }
}
