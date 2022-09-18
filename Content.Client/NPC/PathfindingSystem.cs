using Content.Shared.NPC;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.NPC
{
    public sealed class PathfindingSystem : SharedPathfindingSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public PathfindingDebugMode Modes
        {
            get => _modes;
            set
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (value == PathfindingDebugMode.None)
                {
                    Breadcrumbs.Clear();
                    overlayManager.RemoveOverlay<PathfindingOverlay>();
                }
                else if (!overlayManager.HasOverlay<PathfindingOverlay>())
                {
                    overlayManager.AddOverlay(new PathfindingOverlay(_eyeManager, _inputManager, _mapManager, this));
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PathfindingBreadcrumbsMessage>(OnBreadcrumbs);
            SubscribeNetworkEvent<PathfindingBreadcrumbsRefreshMessage>(OnBreadcrumbsRefresh);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Breadcrumbs.Clear();
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

    internal sealed class PathfindingOverlay : Overlay
    {
        private readonly IEyeManager _eyeManager;
        private readonly IInputManager _inputManager;
        private readonly IMapManager _mapManager;
        private readonly PathfindingSystem _system;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public PathfindingOverlay(IEyeManager eyeManager, IInputManager inputManager, IMapManager mapManager, PathfindingSystem system)
        {
            _eyeManager = eyeManager;
            _inputManager = inputManager;
            _mapManager = mapManager;
            _system = system;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var worldHandle = args.WorldHandle;

            if ((_system.Modes & PathfindingDebugMode.Breadcrumbs) != 0x0)
            {
                var mousePos = _inputManager.MouseScreenPosition;
                var mouseWorldPos = _eyeManager.ScreenToMap(mousePos);

                if (mouseWorldPos.MapId == args.MapId)
                {
                    var aabb = new Box2(mouseWorldPos.Position - Vector2.One / 4f, mouseWorldPos.Position + Vector2.One / 4f);

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

                                var masked = crumb.CollisionMask != 0 || crumb.CollisionLayer != 0;
                                Color color;

                                if ((crumb.Flags & PathfindingBreadcrumbFlag.Space) != 0x0)
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

                                var coordinate = _system.GetCoordinate(crumb.Coordinates);
                                worldHandle.DrawRect(new Box2(coordinate - edge, coordinate + edge), color.WithAlpha(0.25f));
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
