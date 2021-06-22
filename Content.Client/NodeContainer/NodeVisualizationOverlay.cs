using System.Collections.Generic;
using System.Linq;
using Content.Shared.NodeContainer;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.NodeContainer
{
    public sealed class NodeVisualizationOverlay : Overlay
    {
        private readonly NodeGroupSystem _system;
        private readonly IEntityLookup _lookup;
        private readonly IMapManager _mapManager;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        public NodeVisualizationOverlay(NodeGroupSystem system, IEntityLookup lookup, IMapManager mapManager)
        {
            _system = system;
            _lookup = lookup;
            _mapManager = mapManager;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if ((args.Space & OverlaySpace.WorldSpace) != 0)
            {
                DrawWorld(args);
            }
        }

        private void DrawWorld(in OverlayDrawArgs overlayDrawArgs)
        {
            const float nodeSize = 8f / 32;
            const float nodeOffset = 6f / 32;

            var handle = overlayDrawArgs.WorldHandle;

            var map = overlayDrawArgs.Viewport.Eye?.Position.MapId ?? default;
            if (map == MapId.Nullspace)
                return;

            // Group visible nodes by grid tiles.
            var gridIndex =
                new Dictionary<GridId, Dictionary<Vector2i, List<(NodeVis.GroupData, NodeVis.NodeDatum)>>>();

            var totalCount = 0;
            foreach (var entity in _lookup.GetEntitiesIntersecting(map, overlayDrawArgs.WorldBounds))
            {
                if (!_system.Entities.TryGetValue(entity.Uid, out var nodeData))
                    continue;

                var gridId = entity.Transform.GridID;
                var grid = _mapManager.GetGrid(gridId);
                var gridDict = gridIndex.GetOrNew(gridId);

                var tile = gridDict.GetOrNew(grid.TileIndicesFor(entity.Transform.Coordinates));

                foreach (var (group, nodeDatum) in nodeData)
                {
                    if (!_system.Filtered.Contains(group.GroupId))
                    {
                        tile.Add((group, nodeDatum));
                        totalCount += 1;
                    }
                }
            }

            var nodeIndex = new Dictionary<(int, int), NodeRenderData>(totalCount);

            foreach (var (gridId, gridDict) in gridIndex)
            {
                var grid = _mapManager.GetGrid(gridId);
                foreach (var (pos, list) in gridDict)
                {
                    var centerPos = grid.GridTileToWorld(pos).Position;
                    var ordered = list.OrderBy(e => e.Item1.NetId).ThenBy(e => e.Item2.NetId);

                    var offset = -(list.Count - 1) * nodeOffset / 2;

                    foreach (var (group, node) in ordered)
                    {
                        var nodePos = centerPos + (offset, offset);
                        nodeIndex[(group.NetId, node.NetId)] = new NodeRenderData(group, node, nodePos);
                        offset += nodeOffset;
                    }
                }
            }

            foreach (var nodeRenderData in nodeIndex.Values)
            {
                var pos = nodeRenderData.WorldPos;
                var bounds = Box2.CenteredAround(pos, (nodeSize, nodeSize));

                var groupData = nodeRenderData.GroupData;
                var color = groupData.Color;
                color.A = 0.5f;
                handle.DrawRect(bounds, color);

                foreach (var reachable in nodeRenderData.NodeDatum.Reachable)
                {
                    if (nodeIndex.TryGetValue((groupData.NetId, reachable), out var reachDat))
                    {
                        handle.DrawLine(pos, reachDat.WorldPos, color);
                    }
                }
            }
        }

        private sealed class NodeRenderData
        {
            public NodeVis.GroupData GroupData;
            public NodeVis.NodeDatum NodeDatum;
            public Vector2 WorldPos;

            public NodeRenderData(NodeVis.GroupData groupData, NodeVis.NodeDatum nodeDatum, Vector2 worldPos)
            {
                GroupData = groupData;
                NodeDatum = nodeDatum;
                WorldPos = worldPos;
            }
        }
    }
}
