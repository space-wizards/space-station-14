using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;
using System.Text;

namespace Content.Client.Nodes;

public sealed partial class NodeGraphOverlay : Overlay
{
    private const float NodeSize = 8f / 32f;

    private readonly IEntityManager _entMan = default!;
    private readonly IGameTiming _gameTiming = default!;
    private readonly IInputManager _inputMan = default!;
    private readonly IUserInterfaceManager _uiMan = default!;
    private readonly SharedTransformSystem _xformSys = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;
    private readonly List<EntityUid> _visibleNodes = new();
    private readonly HashSet<EntityUid> _hoveredGraphs = new();
    private readonly HashSet<EntityUid> _hoveredNodes = new();
    private ScreenCoordinates _hoverPos = default!;
    private readonly Font _font = default!;

    public NodeGraphOverlay(IEntityManager entMan, IGameTiming gameTiming, IInputManager inputMan, IUserInterfaceManager uiMan, SharedTransformSystem xformSys, IResourceCache cache)
    {
        _entMan = entMan;
        _gameTiming = gameTiming;
        _inputMan = inputMan;
        _uiMan = uiMan;
        _xformSys = xformSys;

        _font = cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _hoveredNodes.Clear();
        _hoveredGraphs.Clear();
        if (_uiMan.CurrentlyHovered is IViewportControl vp)
        {
            _hoverPos = _inputMan.MouseScreenPosition;
            if (!_hoverPos.IsValid)
                return;

            var hoverPos = vp.PixelToMap(_hoverPos.Position);
            var hoverBox = Box2.CenteredAround(hoverPos.Position, new Vector2(NodeSize, NodeSize));
            var nodeEnumerator = _entMan.EntityQueryEnumerator<GraphNodeComponent, TransformComponent>();
            while (nodeEnumerator.MoveNext(out var uid, out var node, out var xform))
            {
                if (xform.MapID != hoverPos.MapId)
                    continue;

                var nodePos = _xformSys.GetWorldPosition(xform);
                if (!hoverBox.Contains(nodePos))
                    continue;

                _hoveredNodes.Add(uid);
                if (node.GraphId is { } hoveredGraphId)
                    _hoveredGraphs.Add(hoveredGraphId);
            }
            return;
        }

        if (_uiMan.CurrentlyHovered is EntityMenuElement element)
        {
            if (element.Entity is not { } hoveredEntity)
                return;
            if (!_entMan.TryGetComponent<GraphNodeComponent>(hoveredEntity, out var hoveredNode))
                return;

            _hoveredNodes.Add(hoveredEntity);
            if (hoveredNode.GraphId is { } hoveredGraphId)
                _hoveredGraphs.Add(hoveredGraphId);
            return;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
            case OverlaySpace.ScreenSpace:
                DrawScreen(args);
                break;
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        const float nodeSize = 8f / 32f;

        var map = args.Viewport.Eye?.Position.MapId ?? default;
        if (map == MapId.Nullspace)
            return;

        var handle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var errSin = MathF.Cos((float) _gameTiming.RealTime.TotalSeconds * 2f);
        errSin *= errSin;

        var nodeQuery = _entMan.GetEntityQuery<GraphNodeComponent>();
        var linkQuery = _entMan.GetEntityQuery<DebugNodeAutolinkerComponent>();
        var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        var nodeEnumerator = _entMan.EntityQueryEnumerator<GraphNodeComponent, TransformComponent>();

        // Render visible edges:
        _visibleNodes.Clear();
        while (nodeEnumerator.MoveNext(out var uid, out var _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var nodePos = _xformSys.GetWorldPosition(xform);
            if (!worldAABB.Contains(nodePos))
                continue;

            _visibleNodes.Add(uid);
        }

        foreach (var uid in _visibleNodes)
        {
            var node = nodeQuery.GetComponent(uid);
            var xform = xformQuery.GetComponent(uid);

            var nodePos = _xformSys.GetWorldPosition(xform);
            var nodeColor = GetColor(node, _hoveredGraphs, errSin);

            handle.DrawRect(Box2.CenteredAround(nodePos, new Vector2(nodeSize, nodeSize)), nodeColor);
            if (linkQuery.TryGetComponent(uid, out var linker))
            {
                var rangeColor = nodeColor.WithAlpha(nodeColor.A * 0.5f);
                handle.DrawCircle(nodePos, linker.BaseRange, rangeColor, filled: false);

                if (linker.HysteresisRange > 0f)
                {
                    rangeColor = rangeColor.WithAlpha(rangeColor.A * 0.5f);
                    handle.DrawCircle(nodePos, linker.BaseRange + linker.HysteresisRange, rangeColor, filled: false);
                }
            }

            foreach (var (edgeId, _) in node.Edges)
            {
                if (!xformQuery.TryGetComponent(edgeId, out var edgeXform))
                    continue;

                if (edgeXform.MapID != mapId)
                    continue;

                var edgePos = _xformSys.GetWorldPosition(edgeXform);
                var midPos = (nodePos + edgePos) / 2f;
                handle.DrawLine(nodePos, midPos, nodeColor); // Edges can form between different graphs so we only draw half of this one.

                if (worldAABB.Contains(edgePos))
                    continue; // We'll get around to drawing the other half of the edge later.

                // Edge node is OOB so we need to draw their half now.
                var edge = nodeQuery.GetComponent(edgeId);
                var edgeColor = GetColor(edge, _hoveredGraphs, errSin);
                handle.DrawLine(edgePos, midPos, edgeColor);
            }
        }
    }

    private void DrawScreen(in OverlayDrawArgs args)
    {
        if (!_hoverPos.IsValid)
            return;
        if (_hoveredNodes.Count <= 0)
            return;

        var sb = new StringBuilder();
        var nodeQuery = _entMan.GetEntityQuery<GraphNodeComponent>();
        foreach (var nodeId in _hoveredNodes)
        {
            if (!nodeQuery.TryGetComponent(nodeId, out var node))
                continue;

            sb.Append($"NodeId: {nodeId}\n");
            sb.Append($"#Edges: {node.Edges.Count}\n");
            sb.Append($"GraphId: {node.GraphId}\n");
            sb.Append($"GraphType: {node.GraphProto}\n");
            args.ScreenHandle.DrawString(_font, _hoverPos.Position, sb.ToString());
            sb.Clear();
        }
    }

    private static Color GetColor(GraphNodeComponent node, HashSet<EntityUid> hoveredGraphs, float sinAlpha)
    {
        const float defaultAlpha = 0.5f;
        const float hoveredAlpha = 0.75f;
        const float unhoveredAlpha = 0.25f;

        float baseAlpha;
        if (hoveredGraphs.Count <= 0)
            baseAlpha = defaultAlpha;
        else if (node.GraphId is { } graphId && hoveredGraphs.Contains(graphId))
            baseAlpha = hoveredAlpha * (1f + sinAlpha) * 0.5f;
        else
            baseAlpha = unhoveredAlpha;

        return node.DebugColor?.WithAlpha(baseAlpha) ?? NodeGraphComponent.DefaultColor.WithAlpha(baseAlpha * sinAlpha);
    }
}
