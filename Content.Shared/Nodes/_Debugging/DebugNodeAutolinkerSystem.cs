using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.Events;

namespace Content.Shared.Nodes.Debugging;

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

    private void OnUpdateEdges(EntityUid uid, DebugNodeAutolinkerComponent comp, ref UpdateEdgesEvent args)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;

        var epicenter = _xformSys.GetWorldPosition(xform);
        var range = comp.BaseRange * comp.BaseRange;

        foreach (var nearId in _lookupSys.GetEntitiesInRange(xform.Coordinates, comp.BaseRange))
        {
            if (nearId == uid)
                continue;
            if (!_nodeQuery.HasComponent(nearId))
                continue;

            var distance = (epicenter - _xformSys.GetWorldPosition(nearId)).LengthSquared();
            if (distance > range)
                continue;

            args.Edges ??= new();
            args.Edges.Add(nearId, comp.Flags);
        }
    }

    private void OnCheckEdge(EntityUid uid, DebugNodeAutolinkerComponent comp, ref CheckEdgeEvent args)
    {
        if (args.Wanted)
            return;

        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;
        if (!_xformQuery.TryGetComponent(args.EdgeId, out var edgeXform))
            return;

        var range = comp.BaseRange + MathF.Max(comp.HysteresisRange, 0f);
        var distance = (_xformSys.GetWorldPosition(xform) - _xformSys.GetWorldPosition(edgeXform)).LengthSquared();
        if (distance > range * range)
            return;

        args.Flags |= comp.Flags;
        args.Wanted = true;
    }
}
