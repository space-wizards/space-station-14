using System.Text;
using Content.Shared.NPC;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.NPC
{
    public sealed class PathfindingSystem : SharedPathfindingSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IResourceCache _cache = default!;

        public PathfindingDebugMode Modes
        {
            get => _modes;
            set
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (value == PathfindingDebugMode.None)
                {
                    Breadcrumbs.Clear();
                    Edges.Clear();
                    overlayManager.RemoveOverlay<PathfindingOverlay>();
                }
                else if (!overlayManager.HasOverlay<PathfindingOverlay>())
                {
                    overlayManager.AddOverlay(new PathfindingOverlay(_eyeManager, _inputManager, _mapManager, _cache, this));
                }

                _modes = value;

                RaiseNetworkEvent(new RequestPathfindingDebugMessage()
                {
                    Mode = _modes,
                });
            }
        }

        private PathfindingDebugMode _modes = PathfindingDebugMode.None;

        // It's debug data IDC if it doesn't support snapshots I just want something fast.
        public Dictionary<EntityUid, Dictionary<Vector2i, List<PathfindingBreadcrumb>>> Breadcrumbs = new();
        public Dictionary<EntityUid, Dictionary<Vector2i, List<List<PathfindingBreadcrumb>>>> Edges = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PathfindingBreadcrumbsMessage>(OnBreadcrumbs);
            SubscribeNetworkEvent<PathfindingBreadcrumbsRefreshMessage>(OnBreadcrumbsRefresh);
            SubscribeNetworkEvent<PathfindingEdgesMessage>(OnEdges);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Breadcrumbs.Clear();
        }

        private void OnEdges(PathfindingEdgesMessage ev)
        {
            var chunks = Edges.GetOrNew(ev.GridUid);
            chunks[ev.Origin] = ev.Edges;
        }

        private void OnBreadcrumbs(PathfindingBreadcrumbsMessage ev)
        {
            Breadcrumbs = ev.Breadcrumbs;
        }

        private void OnBreadcrumbsRefresh(PathfindingBreadcrumbsRefreshMessage ev)
        {
            if (!Breadcrumbs.TryGetValue(ev.GridUid, out var chunks))
                return;

            chunks[ev.Origin] = ev.Data;
        }
    }

    public sealed class PathfindingOverlay : Overlay
    {
        private readonly IEyeManager _eyeManager;
        private readonly IInputManager _inputManager;
        private readonly IMapManager _mapManager;
        private readonly PathfindingSystem _system;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpace;

        private readonly Font _font;

        public PathfindingOverlay(IEyeManager eyeManager, IInputManager inputManager, IMapManager mapManager, IResourceCache cache, PathfindingSystem system)
        {
            _eyeManager = eyeManager;
            _inputManager = inputManager;
            _mapManager = mapManager;
            _system = system;
            _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            switch (args.DrawingHandle)
            {
                case DrawingHandleScreen screenHandle:
                    DrawScreen(args, screenHandle);
                    break;
                case DrawingHandleWorld worldHandle:
                    DrawWorld(args, worldHandle);
                    break;
            }
        }

        private void DrawScreen(OverlayDrawArgs args, DrawingHandleScreen screenHandle)
        {
            var mousePos = _inputManager.MouseScreenPosition;
            var mouseWorldPos = _eyeManager.ScreenToMap(mousePos);
            var aabb = new Box2(mouseWorldPos.Position - SharedPathfindingSystem.ChunkSize, mouseWorldPos.Position + SharedPathfindingSystem.ChunkSize);

            if ((_system.Modes & PathfindingDebugMode.Crumb) != 0x0 &&
                mouseWorldPos.MapId == args.MapId)
            {
                var found = false;

                foreach (var grid in _mapManager.FindGridsIntersecting(mouseWorldPos.MapId, aabb))
                {
                    if (found || !_system.Breadcrumbs.TryGetValue(grid.GridEntityId, out var crumbs))
                        continue;

                    var localAABB = grid.InvWorldMatrix.TransformBox(aabb.Enlarged(float.Epsilon - SharedPathfindingSystem.ChunkSize));
                    var worldMatrix = grid.WorldMatrix;

                    foreach (var chunk in crumbs)
                    {
                        if (found)
                            continue;

                        var origin = chunk.Key * SharedPathfindingSystem.ChunkSize;

                        var chunkAABB = new Box2(origin, origin + SharedPathfindingSystem.ChunkSize);

                        if (!chunkAABB.Intersects(localAABB))
                            continue;

                        PathfindingBreadcrumb? nearest = null;
                        var nearestDistance = float.MaxValue;

                        foreach (var crumb in chunk.Value)
                        {
                            var crumbMapPos = worldMatrix.Transform(_system.GetCoordinate(chunk.Key, crumb.Coordinates));
                            var distance = (crumbMapPos - mouseWorldPos.Position).Length;

                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearest = crumb;
                            }
                        }

                        if (nearest != null)
                        {
                            var text = new StringBuilder();

                            // Sandbox moment
                            var coords = $"Point coordinates: {nearest.Value.Coordinates.ToString()}";
                            var gridCoords =
                                $"Grid coordinates: {_system.GetCoordinate(chunk.Key, nearest.Value.Coordinates).ToString()}";
                            var layer = $"Layer: {nearest.Value.Data.CollisionLayer.ToString()}";
                            var mask = $"Mask: {nearest.Value.Data.CollisionMask.ToString()}";

                            text.AppendLine(coords);
                            text.AppendLine(gridCoords);
                            text.AppendLine(layer);
                            text.AppendLine(mask);
                            text.AppendLine($"Flags:");

                            foreach (var flag in Enum.GetValues<PathfindingBreadcrumbFlag>())
                            {
                                if ((flag & nearest.Value.Data.Flags) == 0x0)
                                    continue;

                                var flagStr = $"- {flag.ToString()}";
                                text.AppendLine(flagStr);
                            }

                            screenHandle.DrawString(_font, mousePos.Position, text.ToString());
                            found = true;
                            break;
                        }
                    }
                }
            }
        }

        private void DrawWorld(OverlayDrawArgs args, DrawingHandleWorld worldHandle)
        {
            var mousePos = _inputManager.MouseScreenPosition;
            var mouseWorldPos = _eyeManager.ScreenToMap(mousePos);
            var aabb = new Box2(mouseWorldPos.Position - Vector2.One / 4f, mouseWorldPos.Position + Vector2.One / 4f);

            if ((_system.Modes & PathfindingDebugMode.Breadcrumbs) != 0x0 &&
                mouseWorldPos.MapId == args.MapId)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mouseWorldPos.MapId, aabb))
                {
                    if (!_system.Breadcrumbs.TryGetValue(grid.GridEntityId, out var crumbs))
                        continue;

                    worldHandle.SetTransform(grid.WorldMatrix);
                    var localAABB = grid.InvWorldMatrix.TransformBox(aabb);

                    foreach (var chunk in crumbs)
                    {
                        var origin = chunk.Key * SharedPathfindingSystem.ChunkSize;

                        var chunkAABB = new Box2(origin, origin + SharedPathfindingSystem.ChunkSize);

                        if (!chunkAABB.Intersects(localAABB))
                            continue;

                        foreach (var crumb in chunk.Value)
                        {
                            if (crumb.Equals(PathfindingBreadcrumb.Invalid))
                            {
                                continue;
                            }

                            const float edge = 1f / SharedPathfindingSystem.SubStep / 4f;

                            var masked = crumb.Data.CollisionMask != 0 || crumb.Data.CollisionLayer != 0;
                            Color color;

                            if ((crumb.Data.Flags & PathfindingBreadcrumbFlag.Space) != 0x0)
                            {
                                color = Color.Green;
                            }
                            else if (masked)
                            {
                                color = Color.Blue;
                            }
                            else
                            {
                                color = Color.Orange;
                            }

                            var coordinate = _system.GetCoordinate(chunk.Key, crumb.Coordinates);
                            worldHandle.DrawRect(new Box2(coordinate - edge, coordinate + edge), color.WithAlpha(0.25f));
                        }
                    }
                }
            }

            if ((_system.Modes & PathfindingDebugMode.Boundary) != 0x0 &&
                mouseWorldPos.MapId == args.MapId)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(args.MapId, aabb))
                {
                    if (!_system.Breadcrumbs.TryGetValue(grid.GridEntityId, out var crumbs))
                        continue;

                    worldHandle.SetTransform(grid.WorldMatrix);
                    var localAABB = grid.InvWorldMatrix.TransformBox(aabb);

                    foreach (var chunk in crumbs)
                    {
                        var origin = chunk.Key * SharedPathfindingSystem.ChunkSize;

                        var chunkAABB = new Box2(origin, origin + SharedPathfindingSystem.ChunkSize);

                        if (!chunkAABB.Intersects(localAABB))
                            continue;

                        foreach (var crumb in chunk.Value)
                        {
                            if (crumb.Equals(PathfindingBreadcrumb.Invalid) ||
                                (crumb.Data.Flags & (PathfindingBreadcrumbFlag.Interior | PathfindingBreadcrumbFlag.External)) != 0x0)
                            {
                                continue;
                            }

                            const float edge = 1f / SharedPathfindingSystem.SubStep / 4f;

                            var color = Color.Green;

                            var coordinate = _system.GetCoordinate(chunk.Key, crumb.Coordinates);
                            worldHandle.DrawRect(new Box2(coordinate - edge, coordinate + edge), color.WithAlpha(0.5f));
                        }
                    }
                }
            }

            if ((_system.Modes & PathfindingDebugMode.Edges) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(args.MapId, aabb))
                {
                    if (!_system.Edges.TryGetValue(grid.GridEntityId, out var edges))
                        continue;

                    worldHandle.SetTransform(grid.WorldMatrix);
                    var localAABB = grid.InvWorldMatrix.TransformBox(aabb);

                    foreach (var chunk in edges)
                    {
                        var origin = chunk.Key * SharedPathfindingSystem.ChunkSize;

                        var chunkAABB = new Box2(origin, origin + SharedPathfindingSystem.ChunkSize);

                        if (!chunkAABB.Intersects(localAABB))
                            continue;

                        foreach (var chain in chunk.Value)
                        {
                            var worldEdges = new ValueList<Vector2>(chain.Count);

                            foreach (var vert in chain)
                            {
                                worldEdges.Add(_system.GetCoordinate(chunk.Key, vert.Coordinates));
                            }

                            // Uhh couldn't find a primitive topology that was implemented for this
                            for (var i = 0; i < worldEdges.Count; i++)
                            {
                                var vert = worldEdges[i];
                                var nextVert = worldEdges[(i + 1) % worldEdges.Count];

                                worldHandle.DrawLine(vert, nextVert, Color.Blue.WithAlpha(0.5f));
                            }
                        }
                    }
                }
            }

            if ((_system.Modes & PathfindingDebugMode.Chunks) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds))
                {
                    if (!_system.Breadcrumbs.TryGetValue(grid.GridEntityId, out var crumbs))
                        continue;

                    worldHandle.SetTransform(grid.WorldMatrix);
                    var localAABB = grid.InvWorldMatrix.TransformBox(args.WorldBounds);

                    foreach (var chunk in crumbs)
                    {
                        var origin = chunk.Key * SharedPathfindingSystem.ChunkSize;

                        var chunkAABB = new Box2(origin, origin + SharedPathfindingSystem.ChunkSize);

                        if (!chunkAABB.Intersects(localAABB))
                            continue;

                        worldHandle.DrawRect(chunkAABB, Color.Red, false);
                    }
                }
            }

            worldHandle.SetTransform(Matrix3.Identity);
        }
    }
}
