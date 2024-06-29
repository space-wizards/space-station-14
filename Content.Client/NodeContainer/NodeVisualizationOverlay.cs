using System.Numerics;
using System.Text;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

        private readonly Dictionary<(int, int), NodeRenderData> _nodeIndex = new();
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, List<(GroupData, NodeDatum)>>> _gridIndex = new ();
        private List<Entity<MapGridComponent>> _grids = new();

        private readonly Font _font;

        private Vector2 _mouseWorldPos = default;
        private (int group, int node)? _hovered;
        private float _time;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        public NodeVisualizationOverlay(
            NodeGroupSystem system,
            EntityLookupSystem lookup,
            IMapManager mapManager,
            IInputManager inputManager,
            IResourceCache cache,
            IEntityManager entityManager)
        {
            _system = system;
            _lookup = lookup;
            _mapManager = mapManager;
            _inputManager = inputManager;
            _entityManager = entityManager;

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
            var mousePos = _inputManager.MouseScreenPosition.Position;
            _mouseWorldPos = args
                .ViewportControl!
                .PixelToMap(mousePos)
                .Position;

            if (_hovered == null)
                return;

            var (groupId, nodeId) = _hovered.Value;

            var group = _system.Groups[groupId];
            var node = _system.NodeLookup[(groupId, nodeId)];


            var xform = _entityManager.GetComponent<TransformComponent>(_entityManager.GetEntity(node.Entity));
            if (!_entityManager.TryGetComponent<MapGridComponent>(xform.GridUid, out var grid))
                return;
            var gridTile = grid.TileIndicesFor(xform.Coordinates);

            var sb = new StringBuilder();
            sb.Append($"entity: {node.Entity}\n");
            sb.Append($"group id: {group.GroupId}\n");
            sb.Append($"node: {node.Name}\n");
            sb.Append($"type: {node.Type}\n");
            sb.Append($"grid pos: {gridTile}\n");
            sb.Append(group.DebugData);

            args.ScreenHandle.DrawString(_font, mousePos + new Vector2(20, -20), sb.ToString());
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

            var cursorBox = Box2.CenteredAround(_mouseWorldPos, new Vector2(nodeSize, nodeSize));

            // Group visible nodes by grid tiles.
            var worldAABB = overlayDrawArgs.WorldAABB;
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

            _grids.Clear();
            _mapManager.FindGridsIntersecting(map, worldAABB, ref _grids);

            foreach (var grid in _grids)
            {
                foreach (var entity in _lookup.GetEntitiesIntersecting(grid, worldAABB))
                {
                    if (!_system.Entities.TryGetValue(entity, out var nodeData))
                        continue;

                    var gridDict = _gridIndex.GetOrNew(grid);
                    var coords = xformQuery.GetComponent(entity).Coordinates;

                    // TODO: This probably shouldn't be capable of returning NaN...
                    if (float.IsNaN(coords.Position.X) || float.IsNaN(coords.Position.Y))
                        continue;

                    var tile = gridDict.GetOrNew(grid.Comp.TileIndicesFor(coords));

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
                var grid = _entityManager.GetComponent<MapGridComponent>(gridId);
                var (_, _, worldMatrix, invMatrix) = _entityManager.GetComponent<TransformComponent>(gridId).GetWorldPositionRotationMatrixWithInv();

                var lCursorBox = invMatrix.TransformBox(cursorBox);
                foreach (var (pos, list) in gridDict)
                {
                    var centerPos = (Vector2) pos + grid.TileSizeHalfVector;
                    list.Sort(NodeDisplayComparer.Instance);

                    var offset = -(list.Count - 1) * nodeOffset / 2;

                    foreach (var (group, node) in list)
                    {
                        var nodePos = centerPos + new Vector2(offset, offset);
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
                    var bounds = Box2.CenteredAround(pos, new Vector2(nodeSize, nodeSize));

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


            handle.SetTransform(Matrix3x2.Identity);
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
