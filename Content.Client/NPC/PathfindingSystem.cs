using Content.Shared.NPC;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.NPC
{
    public sealed class PathfindingSystem : SharedPathfindingSystem
    {
        public PathfindingDebugMode Modes
        {
            get => _modes;
            set
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (value == PathfindingDebugMode.None)
                {
                    overlayManager.RemoveOverlay<DebugPathfindingOverlay>();
                    return;
                }

                _modes = value;

                if (!overlayManager.HasOverlay<DebugPathfindingOverlay>())
                {
                    overlayManager.AddOverlay(new DebugPathfindingOverlay(this));
                }

                if (value )

                RaiseNetworkEvent(new RequestPathfindingBreadcrumbsMessage());
            }
        }

        private PathfindingDebugMode _modes = PathfindingDebugMode.None;

        // It's debug data IDC if it doesn't support snapshots I just want something fast.
        public Dictionary<EntityUid, Dictionary<Vector2i, List<PathfindingBreadcrumb>>> Breadcrumbs = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PathfindingBreadcrumbsMessage>(OnBreadcrumbs);
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
    }

    internal sealed class DebugPathfindingOverlay : Overlay
    {
        private PathfindingSystem _system;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public DebugPathfindingOverlay(PathfindingSystem system)
        {
            _system = system;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            return;
        }
    }
}
