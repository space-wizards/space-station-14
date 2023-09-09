using Content.Shared.Nodes.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.Nodes;

public sealed partial class NodeGraphOverlay : Overlay
{
    private readonly IEntityManager _entMan = default!;
    private readonly SharedTransformSystem _xformSys = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NodeGraphOverlay(IEntityManager entMan, SharedTransformSystem xformSys)
    {
        _entMan = entMan;
        _xformSys = xformSys;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        const float nodeSize = 8f / 32f;

        var map = args.Viewport.Eye?.Position.MapId ?? default;
        if (map == MapId.Nullspace)
            return;

        var handle = args.WorldHandle;
        var worldAABB = args.WorldAABB;

        // TODO: Draw edges
        var nodeEnumerator = _entMan.EntityQueryEnumerator<GraphNodeComponent, TransformComponent>();
        while (nodeEnumerator.MoveNext(out var _, out var node, out var xform))
        {
            var nodePos = _xformSys.GetWorldPosition(xform);
            if (!worldAABB.Contains(nodePos))
                continue;

            handle.DrawRect(Box2.CenteredAround(nodePos, new Vector2(nodeSize, nodeSize)), Color.White);
            foreach (var edgeId in node.Edges)
            {
                var edgePos = _xformSys.GetWorldPosition(edgeId);
                handle.DrawLine(nodePos, edgePos, Color.White);
            }
        }
    }
}
