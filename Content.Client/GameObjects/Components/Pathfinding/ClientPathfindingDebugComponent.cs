using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Pathfinding;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timers;

namespace Content.Client.GameObjects.Components.Pathfinding
{
    // Component to receive the route data and an overlay to show it
    // Most of this is duplicated with MobTooltip but I doubt it will be duplicated again. If it is then abstract it out.
    [RegisterComponent]
    internal sealed class ClientPathfindingDebugComponent : SharedPathfindingDebugComponent
    {
        private static int _modes;

        public static void DisableAll()
        {
            _modes = 0;
            Toggle?.Invoke();
        }

        public static void EnableTooltip(PathfindingDebugMode mode)
        {
            _modes |= (int) mode;
            Toggle?.Invoke();
        }

        public static void DisableTooltip(PathfindingDebugMode mode)
        {
            _modes &= ~(int) mode;
            Toggle?.Invoke();
        }

        public static void ToggleTooltip(PathfindingDebugMode mode)
        {
            if ((_modes & (int) mode) != 0)
            {
                DisableTooltip(mode);
            }
            else
            {
                EnableTooltip(mode);
            }
        }

        private static event Action Toggle;

        private DebugPathfindingOverlay _overlay;
        private float _routeDuration = 4.0f; // How long before we remove a route from the overlay

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case JpsRouteMessage route:
                    ReceivedJpsRoute(route);
                    break;
                case AStarRouteMessage route:
                    ReceivedAStarRoute(route);
                    break;
                case PathfindingGraphMessage graph:
                    if ((_modes & (int) PathfindingDebugMode.Graph) != 0)
                    {
                        ReceivedGraph(graph);
                    }
                    break;
            }
        }

        private void ToggleOverlay()
        {
            if (_modes != 0)
            {
                if ((_modes & (int) PathfindingDebugMode.Graph) != 0)
                {
                    SendNetworkMessage(new RequestPathfindingGraphMessage());
                }
                else
                {
                    _overlay?.Graph.Clear();
                }

                if (_overlay == null)
                {
                    var overlayManager = IoCManager.Resolve<IOverlayManager>();
                    _overlay = new DebugPathfindingOverlay();
                    overlayManager.AddOverlay(_overlay);
                }
            }
            else
            {
                if (_overlay == null)
                {
                    return;
                }

                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                overlayManager.RemoveOverlay(_overlay.ID);
                _overlay = null;
                return;
            }

            _overlay.Modes = _modes;
        }

        public override void OnAdd()
        {
            base.OnAdd();
            Toggle += ToggleOverlay;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Toggle -= ToggleOverlay;
            if (_overlay == null)
            {
                return;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.RemoveOverlay(_overlay.ID);
            _overlay = null;
        }

        private void ReceivedAStarRoute(AStarRouteMessage routeMessage)
        {
            if (_overlay == null)
            {
                return;
            }
            _overlay.AStarRoutes.Add(routeMessage);
            Timer.Spawn(TimeSpan.FromSeconds(_routeDuration), () =>
            {
                if (_overlay == null) return;
                _overlay.AStarRoutes.Remove(routeMessage);
            });
        }

        private void ReceivedJpsRoute(JpsRouteMessage routeMessage)
        {
            if (_overlay == null)
            {
                return;
            }
            _overlay.JpsRoutes.Add(routeMessage);
            Timer.Spawn(TimeSpan.FromSeconds(_routeDuration), () =>
            {
                if (_overlay == null) return;
                _overlay.JpsRoutes.Remove(routeMessage);
            });
        }

        private void ReceivedGraph(PathfindingGraphMessage message)
        {
            _overlay?.UpdateGraph(message.Graph);
        }
    }

    internal sealed class DebugPathfindingOverlay : Overlay
    {
        // TODO: Add a box like the debug one and show the most recent path stuff
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public int Modes { get; set; } = 0;

        // Graph debugging
        public readonly Dictionary<int, List<Vector2>> Graph = new Dictionary<int, List<Vector2>>();
        private readonly Dictionary<int, Color> _graphColors = new Dictionary<int, Color>();

        // Route debugging
        // As each pathfinder is very different you'll likely want to draw them completely different
        public readonly List<AStarRouteMessage> AStarRoutes = new List<AStarRouteMessage>();
        public readonly List<JpsRouteMessage> JpsRoutes = new List<JpsRouteMessage>();

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
                _graphColors[chunk] = new Color(robustRandom.NextFloat(), robustRandom.NextFloat(), robustRandom.NextFloat(), 0.3f);
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
                    if ((route.Route.Contains(tile) && (Modes & (int) PathfindingDebugMode.Route) != 0) || !viewport.Contains(tile))
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
                    if ((route.Route.Contains(tile) && (Modes & (int) PathfindingDebugMode.Route) != 0) || !viewport.Contains(tile))
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

            if ((Modes & (int) PathfindingDebugMode.Route) != 0)
            {
                DrawAStarRoutes(screenHandle);
                DrawJpsRoutes(screenHandle);
            }

            if ((Modes & (int) PathfindingDebugMode.Nodes) != 0)
            {
                DrawAStarNodes(screenHandle);
                DrawJpsNodes(screenHandle);
            }

            if ((Modes & (int) PathfindingDebugMode.Graph) != 0)
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
}
