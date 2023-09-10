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
    private readonly Font _font = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;
    private readonly List<EntityUid> _visibleNodes = new();
    private readonly HashSet<EntityUid> _hoveredGraphs = new();
    private readonly HashSet<EntityUid> _hoveredNodes = new();
    private ScreenCoordinates _hoverPos = default!;
    private float _sinAlpha = 1f;

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

        _sinAlpha = MathF.Cos((float) _gameTiming.RealTime.TotalSeconds * 2f);
        _sinAlpha *= _sinAlpha;
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
        var map = args.Viewport.Eye?.Position.MapId ?? default;
        if (map == MapId.Nullspace)
            return;

        var handle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;

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
            var nodeColor = GetColor(node);

            handle.DrawRect(Box2.CenteredAround(nodePos, new Vector2(NodeSize, NodeSize)), nodeColor);
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

            foreach (var (edgeId, edgeFlags) in node.Edges)
            {
                if (!xformQuery.TryGetComponent(edgeId, out var edgeXform))
                    continue;

                if (edgeXform.MapID != mapId)
                    continue;

                var canMerge = (edgeFlags & EdgeFlags.NoMerge) == EdgeFlags.None;
                var edgePos = _xformSys.GetWorldPosition(edgeXform);
                DrawHalfEdge(handle, nodePos, edgePos, nodeColor, canMerge: canMerge);

                if (worldAABB.Contains(edgePos))
                    continue; // We'll get around to drawing the other half of the edge later.

                // Edge node is OOB so we need to draw their half now.
                var edge = nodeQuery.GetComponent(edgeId);
                var edgeColor = GetColor(edge);
                DrawHalfEdge(handle, edgePos, nodePos, edgeColor, canMerge: canMerge);
            }
        }
    }

    private void DrawScreen(in OverlayDrawArgs args)
    {
        if (!_hoverPos.IsValid)
            return;
        if (_hoveredNodes.Count <= 0)
            return;

        var offset = new Vector2(0, 0);
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
            var size = args.ScreenHandle.DrawString(_font, _hoverPos.Position + offset, sb.ToString(), GetColor(node));
            offset.Y += size.Y;
            sb.Clear();
        }
    }

    private Color GetColor(GraphNodeComponent node)
    {
        const float defaultAlpha = 0.5f;
        const float hoveredAlpha = 0.75f;
        const float unhoveredAlpha = 0.25f;

        float baseAlpha;
        if (_hoveredGraphs.Count <= 0)
            baseAlpha = defaultAlpha;
        else if (node.GraphId is { } graphId && _hoveredGraphs.Contains(graphId))
            baseAlpha = hoveredAlpha * (1f + _sinAlpha) * 0.5f;
        else
            baseAlpha = unhoveredAlpha;

        return node.DebugColor?.WithAlpha(baseAlpha) ?? NodeGraphComponent.DefaultColor.WithAlpha(baseAlpha * _sinAlpha);
    }

    private static void DrawHalfEdge(DrawingHandleWorld handle, Vector2 from, Vector2 to, Color color, bool canMerge)
    {
        var endPos = (from + to) / 2f;

        if (!canMerge)
        {
            var shortPos = from + (to - from) * 0.45f;
            handle.DrawLine(shortPos, endPos, Color.Black.WithAlpha(color.A));
            endPos = shortPos;
        }

        handle.DrawLine(from, endPos, color);
    }
}
