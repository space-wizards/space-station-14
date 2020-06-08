using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.AI;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timers;

namespace Content.Client.GameObjects.EntitySystems.AI
{
#if DEBUG
    public class ClientPathfindingDebugSystem : EntitySystem
    {
        private PathfindingDebugMode _modes = PathfindingDebugMode.None;
        private float _routeDuration = 4.0f; // How long before we remove a route from the overlay
        private DebugPathfindingOverlay _overlay;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SharedAiDebug.AStarRouteMessage>(HandleAStarRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.JpsRouteMessage>(HandleJpsRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.PathfindingGraphMessage>(HandleGraphMessage);
        }

        private void HandleAStarRouteMessage(SharedAiDebug.AStarRouteMessage message)
        {
            if ((_modes & PathfindingDebugMode.Nodes) != 0 ||
                (_modes & PathfindingDebugMode.Route) != 0)
            {
                _overlay.AStarRoutes.Add(message);
                Timer.Spawn(TimeSpan.FromSeconds(_routeDuration), () =>
                {
                    if (_overlay == null) return;
                    _overlay.AStarRoutes.Remove(message);
                });
            }
        }

        private void HandleJpsRouteMessage(SharedAiDebug.JpsRouteMessage message)
        {
            if ((_modes & PathfindingDebugMode.Nodes) != 0 ||
                    (_modes & PathfindingDebugMode.Route) != 0)
            {
                _overlay.JpsRoutes.Add(message);
                Timer.Spawn(TimeSpan.FromSeconds(_routeDuration), () =>
                {
                    if (_overlay == null) return;
                    _overlay.JpsRoutes.Remove(message);
                });
            }
        }

        private void HandleGraphMessage(SharedAiDebug.PathfindingGraphMessage message)
        {
            if ((_modes & PathfindingDebugMode.Graph) != 0)
            {
                _overlay.UpdateGraph(message.Graph);
            }
        }

        private void EnableOverlay()
        {
            if (_overlay != null)
            {
                return;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            _overlay = new DebugPathfindingOverlay {Modes = _modes};
            overlayManager.AddOverlay(_overlay);
        }

        private void DisableOverlay()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.Modes = 0;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.RemoveOverlay(_overlay.ID);
            _overlay = null;
        }

        public void Disable()
        {
            _modes = PathfindingDebugMode.None;
            DisableOverlay();
        }


        private void EnableMode(PathfindingDebugMode tooltip)
        {
            _modes |= tooltip;
            if (_modes != 0)
            {
                EnableOverlay();
            }
            _overlay.Modes = _modes;

            if (tooltip == PathfindingDebugMode.Graph)
            {
                var systemMessage = new SharedAiDebug.RequestPathfindingGraphMessage();
                EntityManager.EntityNetManager.SendSystemNetworkMessage(systemMessage);
            }
        }

        private void DisableMode(PathfindingDebugMode mode)
        {
            _modes &= ~mode;
            if (_modes == 0)
            {
                DisableOverlay();
            }
            else
            {
                _overlay.Modes = _modes;
            }
        }

        public void ToggleTooltip(PathfindingDebugMode mode)
        {
            if ((_modes & mode) != 0)
            {
                DisableMode(mode);
            }
            else
            {
                EnableMode(mode);
            }
        }
    }

    internal sealed class DebugPathfindingOverlay : Overlay
    {
        // TODO: Add a box like the debug one and show the most recent path stuff
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public PathfindingDebugMode Modes { get; set; } = PathfindingDebugMode.None;

        // Graph debugging
        public readonly Dictionary<int, List<Vector2>> Graph = new Dictionary<int, List<Vector2>>();
        private readonly Dictionary<int, Color> _graphColors = new Dictionary<int, Color>();

        // Route debugging
        // As each pathfinder is very different you'll likely want to draw them completely different
        public readonly List<SharedAiDebug.AStarRouteMessage> AStarRoutes = new List<SharedAiDebug.AStarRouteMessage>();
        public readonly List<SharedAiDebug.JpsRouteMessage> JpsRoutes = new List<SharedAiDebug.JpsRouteMessage>();

        public DebugPathfindingOverlay() : base(nameof(DebugPathfindingOverlay))
        {
            Shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
        }

        public void UpdateGraph(Dictionary<int, List<Vector2>> graph)
        {
            Graph.Clear();
            _graphColors.Clear();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            foreach (var (chunk, nodes) in graph)
            {
                Graph[chunk] = nodes;
                _graphColors[chunk] = new Color(robustRandom.NextFloat(), robustRandom.NextFloat(),
                    robustRandom.NextFloat(), 0.3f);
            }
        }

        private void DrawGraph(DrawingHandleScreen screenHandle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            var viewport = IoCManager.Resolve<IEyeManager>().GetWorldViewport();

            foreach (var (chunk, nodes) in Graph)
            {
                foreach (var tile in nodes)
                {
                    if (!viewport.Contains(tile)) continue;

                    var screenTile = eyeManager.WorldToScreen(tile);

                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, _graphColors[chunk]);
                }
            }
        }

        #region pathfinder

        private void DrawAStarRoutes(DrawingHandleScreen screenHandle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            var viewport = eyeManager.GetWorldViewport();

            foreach (var route in AStarRoutes)
            {
                // Draw box on each tile of route
                foreach (var position in route.Route)
                {
                    if (!viewport.Contains(position)) continue;
                    var screenTile = eyeManager.WorldToScreen(position);
                    // worldHandle.DrawLine(position, nextWorld.Value, Color.Blue);
                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);
                    screenHandle.DrawRect(box, Color.Orange.WithAlpha(0.25f));
                }
            }
        }

        private void DrawAStarNodes(DrawingHandleScreen screenHandle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            var viewport = eyeManager.GetWorldViewport();

            foreach (var route in AStarRoutes)
            {
                var highestgScore = route.GScores.Values.Max();

                foreach (var (tile, score) in route.GScores)
                {
                    if ((route.Route.Contains(tile) && (Modes & PathfindingDebugMode.Route) != 0) ||
                        !viewport.Contains(tile))
                    {
                        continue;
                    }

                    var screenTile = eyeManager.WorldToScreen(tile);

                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, new Color(
                        0.0f,
                        score / highestgScore,
                        1.0f - (score / highestgScore),
                        0.1f));
                }
            }
        }

        private void DrawJpsRoutes(DrawingHandleScreen screenHandle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            var viewport = eyeManager.GetWorldViewport();

            foreach (var route in JpsRoutes)
            {
                // Draw box on each tile of route
                foreach (var position in route.Route)
                {
                    if (!viewport.Contains(position)) continue;
                    var screenTile = eyeManager.WorldToScreen(position);
                    // worldHandle.DrawLine(position, nextWorld.Value, Color.Blue);
                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);
                    screenHandle.DrawRect(box, Color.Orange.WithAlpha(0.25f));
                }
            }
        }

        private void DrawJpsNodes(DrawingHandleScreen screenHandle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            var viewport = eyeManager.GetWorldViewport();

            foreach (var route in JpsRoutes)
            {
                foreach (var tile in route.JumpNodes)
                {
                    if ((route.Route.Contains(tile) && (Modes & PathfindingDebugMode.Route) != 0) ||
                        !viewport.Contains(tile))
                    {
                        continue;
                    }

                    var screenTile = eyeManager.WorldToScreen(tile);

                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, new Color(
                        0.0f,
                        1.0f,
                        0.0f,
                        0.2f));
                }
            }
        }

        #endregion

        protected override void Draw(DrawingHandleBase handle)
        {
            if (Modes == 0)
            {
                return;
            }

            var screenHandle = (DrawingHandleScreen) handle;

            if ((Modes & PathfindingDebugMode.Route) != 0)
            {
                DrawAStarRoutes(screenHandle);
                DrawJpsRoutes(screenHandle);
            }

            if ((Modes & PathfindingDebugMode.Nodes) != 0)
            {
                DrawAStarNodes(screenHandle);
                DrawJpsNodes(screenHandle);
            }

            if ((Modes & PathfindingDebugMode.Graph) != 0)
            {
                DrawGraph(screenHandle);
            }
        }
    }

    [Flags]
    public enum PathfindingDebugMode {
        None = 0,
        Route = 1 << 0,
        Graph = 1 << 1,
        Nodes = 1 << 2,
    }
#endif
}
