using System.Text;
using Content.Client.Examine;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.NodeContainer.NodeVis;

namespace Content.Client.NodeContainer
{
    public sealed class NodeVisualizationOverlay : Overlay
    {

        private readonly NodeGroupSystem _system;
        private readonly EntityLookupSystem _lookup;
        private readonly IMapManager _mapManager;
        private readonly IInputManager _inputManager;
        private readonly IEntityManager _entityManager;
        private readonly IUserInterfaceManager _userInterface = default!;

        private readonly Dictionary<(int, int), NodeRenderData> _nodeIndex = new();
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, List<(GroupData, NodeDatum)>>> _gridIndex = new ();

        private readonly Font _font;

        private Vector2 _mouseWorldPos = default;
        private (int group, int node)? _hovered;
        private float _time;

        private Popup? _popup = null;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        public NodeVisualizationOverlay(
            NodeGroupSystem system,
            EntityLookupSystem lookup,
            IMapManager mapManager,
            IInputManager inputManager,
            IResourceCache cache,
            IEntityManager entityManager,
            IUserInterfaceManager userInterface)
        {
            _system = system;
            _lookup = lookup;
            _mapManager = mapManager;
            _inputManager = inputManager;
            _entityManager = entityManager;
            _userInterface = userInterface;

            _font = cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if ((args.Space & OverlaySpace.WorldSpace) != 0)
            {
                DrawWorld(args);
            }
            else if ((args.Space & OverlaySpace.ScreenSpace) != 0)
            {
                DrawScreen(args);
            }
        }

        private void DrawScreen(in OverlayDrawArgs args)
        {
            var mousePos = _userInterface.MousePositionScaled;
            _mouseWorldPos = args
                .ViewportControl!
                .ScreenToMap(mousePos.Position)
                .Position;

            if (_hovered == null)
            {
                if (_popup is not null)
                {
                    _popup.Dispose();
                    _popup = null;
                }
                return;
            }

            var (groupId, nodeId) = _hovered.Value;

            var group = _system.Groups[groupId];
            var node = _system.NodeLookup[(groupId, nodeId)];


            var xform = _entityManager.GetComponent<TransformComponent>(node.Entity);
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return;
            var gridTile = grid.TileIndicesFor(xform.Coordinates);

            var sb = new StringBuilder();
            sb.Append($"entity: {node.Entity}\n");
            sb.Append($"group id: {group.GroupId}\n");
            sb.Append($"node: {node.Name}\n");
            sb.Append($"type: {node.Type}\n");
            sb.Append($"grid pos: {gridTile}\n");
            sb.Append(group.DebugData);

            if (_popup is null)
            {
                _popup = new Popup() { MaxWidth = 400 };
                _userInterface.ModalRoot.AddChild(_popup);
                var newPanel = new PanelContainer() {Name = "NodeVisPopupPanel"};
                newPanel.AddStyleClass(ExamineSystem.StyleClassEntityTooltip);
                newPanel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
                _popup!.AddChild(newPanel);
                _popup.NameScope = new NameScope();
                _popup.NameScope.Register(newPanel.Name, newPanel);

                var richLabel = new RichTextLabel() { Margin = new Thickness(4, 4, 0, 4), Name = "Label"};
                newPanel.AddChild(richLabel);

                newPanel.NameScope = new NameScope();
                newPanel.NameScope.Register(richLabel.Name, richLabel);
            }

            var panel = _popup.FindControl<PanelContainer>("NodeVisPopupPanel");
            var text = panel.FindControl<RichTextLabel>("Label");
            text.SetMessage(sb.ToString());

            panel.Measure(Vector2.Infinity);
            var size = Vector2.ComponentMax((300, 0), panel.DesiredSize);

            _popup.Open(UIBox2.FromDimensions(mousePos.Position + (20, -20), size));
        }

        private void DrawWorld(in OverlayDrawArgs overlayDrawArgs)
        {
            const float nodeSize = 8f / 32;
            const float nodeOffset = 6f / 32;

            var handle = overlayDrawArgs.WorldHandle;

            var map = overlayDrawArgs.Viewport.Eye?.Position.MapId ?? default;
            if (map == MapId.Nullspace)
                return;

            _hovered = default;

            var cursorBox = Box2.CenteredAround(_mouseWorldPos, (nodeSize, nodeSize));

            // Group visible nodes by grid tiles.
            var worldAABB = overlayDrawArgs.WorldAABB;
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in _mapManager.FindGridsIntersecting(map, worldAABB))
            {
                foreach (var entity in _lookup.GetEntitiesIntersecting(grid.GridEntityId, worldAABB))
                {
                    if (!_system.Entities.TryGetValue(entity, out var nodeData))
                        continue;

                    var gridDict = _gridIndex.GetOrNew(grid.GridEntityId);
                    var coords = xformQuery.GetComponent(entity).Coordinates;

                    // TODO: This probably shouldn't be capable of returning NaN...
                    if (float.IsNaN(coords.Position.X) || float.IsNaN(coords.Position.Y))
                        continue;

                    var tile = gridDict.GetOrNew(grid.TileIndicesFor(coords));

                    foreach (var (group, nodeDatum) in nodeData)
                    {
                        if (!_system.Filtered.Contains(group.GroupId))
                        {
                            tile.Add((group, nodeDatum));
                        }
                    }
                }
            }

            foreach (var (gridId, gridDict) in _gridIndex)
            {
                var grid = _mapManager.GetGrid(gridId);
                var (_, _, worldMatrix, invMatrix) = _entityManager.GetComponent<TransformComponent>(grid.GridEntityId).GetWorldPositionRotationMatrixWithInv();

                var lCursorBox = invMatrix.TransformBox(cursorBox);
                foreach (var (pos, list) in gridDict)
                {
                    var centerPos = (Vector2) pos + grid.TileSize / 2f;
                    list.Sort(NodeDisplayComparer.Instance);

                    var offset = -(list.Count - 1) * nodeOffset / 2;

                    foreach (var (group, node) in list)
                    {
                        var nodePos = centerPos + (offset, offset);
                        if (lCursorBox.Contains(nodePos))
                            _hovered = (group.NetId, node.NetId);

                        _nodeIndex[(group.NetId, node.NetId)] = new NodeRenderData(group, node, nodePos);
                        offset += nodeOffset;
                    }
                }

                handle.SetTransform(worldMatrix);

                foreach (var nodeRenderData in _nodeIndex.Values)
                {
                    var pos = nodeRenderData.NodePos;
                    var bounds = Box2.CenteredAround(pos, (nodeSize, nodeSize));

                    var groupData = nodeRenderData.GroupData;
                    var color = groupData.Color;

                    if (!_hovered.HasValue)
                        color.A = 0.5f;
                    else if (_hovered.Value.group != groupData.NetId)
                        color.A = 0.2f;
                    else
                        color.A = 0.75f + MathF.Sin(_time * 4) * 0.25f;

                    handle.DrawRect(bounds, color);

                    foreach (var reachable in nodeRenderData.NodeDatum.Reachable)
                    {
                        if (_nodeIndex.TryGetValue((groupData.NetId, reachable), out var reachDat))
                        {
                            handle.DrawLine(pos, reachDat.NodePos, color);
                        }
                    }
                }

                _nodeIndex.Clear();
            }


            handle.SetTransform(Matrix3.Identity);
            _gridIndex.Clear();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _time += args.DeltaSeconds;
        }

        private sealed class NodeDisplayComparer : IComparer<(GroupData, NodeDatum)>
        {
            public static readonly NodeDisplayComparer Instance = new();

            public int Compare((GroupData, NodeDatum) x, (GroupData, NodeDatum) y)
            {
                var (groupX, nodeX) = x;
                var (groupY, nodeY) = y;

                var cmp = groupX.NetId.CompareTo(groupY.NetId);
                if (cmp != 0)
                    return cmp;

                return nodeX.NetId.CompareTo(nodeY.NetId);
            }
        }

        private sealed class NodeRenderData
        {
            public GroupData GroupData;
            public NodeDatum NodeDatum;
            public Vector2 NodePos;

            public NodeRenderData(GroupData groupData, NodeDatum nodeDatum, Vector2 nodePos)
            {
                GroupData = groupData;
                NodeDatum = nodeDatum;
                NodePos = nodePos;
            }
        }
    }
}
