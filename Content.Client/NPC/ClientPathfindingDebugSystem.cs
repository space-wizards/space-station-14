using System.Linq;
using Content.Shared.AI;
using Content.Shared.NPC;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.NPC
{
    public sealed class ClientPathfindingDebugSystem : EntitySystem
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
                    overlayManager.AddOverlay(new DebugPathfindingOverlay());
                }

                RaiseNetworkEvent(new RequestPathfindingBreadcrumbsMessage());
            }
        }

        private PathfindingDebugMode _modes = PathfindingDebugMode.None;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PathfindingBreadcrumbsMessage>(OnBreadcrumbs);
        }

        private void OnBreadcrumbs(PathfindingBreadcrumbsMessage ev)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class DebugPathfindingOverlay : Overlay
    {
        private ClientPathfindingDebugSystem _system;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public DebugPathfindingOverlay(ClientPathfindingDebugSystem system)
        {
            _system = system;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            return;
        }
    }

    [Flags]
    public enum PathfindingDebugMode : byte
    {
        None = 0,
        Breadcrumbs = 1 << 0,
    }
}
