using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.AI;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.EntitySystems.AI
{
#if DEBUG
    public class ClientPathfindingDebugSystem : EntitySystem
    {
        private PathfindingDebugMode _modes = PathfindingDebugMode.None;
        private float _routeDuration = 4.0f; // How long before we remove a route from the overlay
        private DebugPathfindingOverlay? _overlay;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SharedAiDebug.AStarRouteMessage>(HandleAStarRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.JpsRouteMessage>(HandleJpsRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.PathfindingGraphMessage>(HandleGraphMessage);
            SubscribeNetworkEvent<SharedAiDebug.ReachableChunkRegionsDebugMessage>(HandleRegionsMessage);
            SubscribeNetworkEvent<SharedAiDebug.ReachableCacheDebugMessage>(HandleCachedRegionsMessage);
            // I'm lazy
            EnableOverlay();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            DisableOverlay();
        }

        private void HandleAStarRouteMessage(SharedAiDebug.AStarRouteMessage message)
        {
            if ((_modes & PathfindingDebugMode.Nodes) != 0 ||
                (_modes & PathfindingDebugMode.Route) != 0)
            {
                _overlay?.AStarRoutes.Add(message);
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
                _overlay?.JpsRoutes.Add(message);
                Timer.Spawn(TimeSpan.FromSeconds(_routeDuration), () =>
                {
                    if (_overlay == null) return;
                    _overlay.JpsRoutes.Remove(message);
                });
            }
        }

        private void HandleGraphMessage(SharedAiDebug.PathfindingGraphMessage message)
        {
            EnableOverlay().UpdateGraph(message.Graph);
        }

        private void HandleRegionsMessage(SharedAiDebug.ReachableChunkRegionsDebugMessage message)
        {
            EnableOverlay().UpdateRegions(message.GridId, message.Regions);
        }

        private void HandleCachedRegionsMessage(SharedAiDebug.ReachableCacheDebugMessage message)
        {
            EnableOverlay().UpdateCachedRegions(message.GridId, message.Regions, message.Cached);
        }

        private DebugPathfindingOverlay EnableOverlay()
        {
            if (_overlay != null)
            {
                return _overlay;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            _overlay = new DebugPathfindingOverlay {Modes = _modes};
            overlayManager.AddOverlay(_overlay);

            return _overlay;
        }

        private void DisableOverlay()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.Modes = 0;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.RemoveOverlay(_overlay);
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

            if (_overlay != null)
            {
                _overlay.Modes = _modes;
            }

            if (tooltip == PathfindingDebugMode.Graph)
            {
                RaiseNetworkEvent(new SharedAiDebug.RequestPathfindingGraphMessage());
            }

            if (tooltip == PathfindingDebugMode.Regions)
            {
                RaiseNetworkEvent(new SharedAiDebug.SubscribeReachableMessage());
            }

            // TODO: Request region graph, although the client system messages didn't seem to be going through anymore
            // So need further investigation.
        }

        private void DisableMode(PathfindingDebugMode mode)
        {
            if (mode == PathfindingDebugMode.Regions && (_modes & PathfindingDebugMode.Regions) != 0)
            {
                RaiseNetworkEvent(new SharedAiDebug.UnsubscribeReachableMessage());
            }

            _modes &= ~mode;
            if (_modes == 0)
            {
                DisableOverlay();
            }
            else if (_overlay != null)
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
        private readonly IEyeManager _eyeManager;
        private readonly IPlayerManager _playerManager;

        // TODO: Add a box like the debug one and show the most recent path stuff
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly ShaderInstance _shader;

        public PathfindingDebugMode Modes { get; set; } = PathfindingDebugMode.None;

        // Graph debugging
        public readonly Dictionary<int, List<Vector2>> Graph = new();
        private readonly Dictionary<int, Color> _graphColors = new();

        // Cached regions
        public readonly Dictionary<GridId, Dictionary<int, List<Vector2>>> CachedRegions =
                    new();

        private readonly Dictionary<GridId, Dictionary<int, Color>> _cachedRegionColors =
                     new();

        // Regions
        public readonly Dictionary<GridId, Dictionary<int, Dictionary<int, List<Vector2>>>> Regions =
                    new();

        private readonly Dictionary<GridId, Dictionary<int, Dictionary<int, Color>>> _regionColors =
                     new();

        // Route debugging
        // As each pathfinder is very different you'll likely want to draw them completely different
        public readonly List<SharedAiDebug.AStarRouteMessage> AStarRoutes = new();
        public readonly List<SharedAiDebug.JpsRouteMessage> JpsRoutes = new();

        public DebugPathfindingOverlay()
        {
            _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
            _eyeManager = IoCManager.Resolve<IEyeManager>();
            _playerManager = IoCManager.Resolve<IPlayerManager>();
        }

        #region Graph
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

        private void DrawGraph(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            foreach (var (chunk, nodes) in Graph)
            {
                foreach (var tile in nodes)
                {
                    if (!viewport.Contains(tile)) continue;

                    var screenTile = _eyeManager.WorldToScreen(tile);
                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, _graphColors[chunk]);
                }
            }
        }
        #endregion

        #region Regions
        //Server side debugger should increment every region
        public void UpdateCachedRegions(GridId gridId, Dictionary<int, List<Vector2>> messageRegions, bool cached)
        {
            if (!CachedRegions.ContainsKey(gridId))
            {
                CachedRegions.Add(gridId, new Dictionary<int, List<Vector2>>());
                _cachedRegionColors.Add(gridId, new Dictionary<int, Color>());
            }

            foreach (var (region, nodes) in messageRegions)
            {
                CachedRegions[gridId][region] = nodes;
                if (cached)
                {
                    _cachedRegionColors[gridId][region] = Color.Blue.WithAlpha(0.3f);
                }
                else
                {
                    _cachedRegionColors[gridId][region] = Color.Green.WithAlpha(0.3f);
                }

                Timer.Spawn(3000, () =>
                {
                    if (CachedRegions[gridId].ContainsKey(region))
                    {
                        CachedRegions[gridId].Remove(region);
                        _cachedRegionColors[gridId].Remove(region);
                    }
                });
            }
        }

        private void DrawCachedRegions(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            var attachedEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (attachedEntity == null || !CachedRegions.TryGetValue(attachedEntity.Transform.GridID, out var entityRegions))
            {
                return;
            }

            foreach (var (region, nodes) in entityRegions)
            {
                foreach (var tile in nodes)
                {
                    if (!viewport.Contains(tile)) continue;

                    var screenTile = _eyeManager.WorldToScreen(tile);
                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, _cachedRegionColors[attachedEntity.Transform.GridID][region]);
                }
            }
        }

        public void UpdateRegions(GridId gridId, Dictionary<int, Dictionary<int, List<Vector2>>> messageRegions)
        {
            if (!Regions.ContainsKey(gridId))
            {
                Regions.Add(gridId, new Dictionary<int, Dictionary<int, List<Vector2>>>());
                _regionColors.Add(gridId, new Dictionary<int, Dictionary<int, Color>>());
            }

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            foreach (var (chunk, regions) in messageRegions)
            {
                Regions[gridId][chunk] = new Dictionary<int, List<Vector2>>();
                _regionColors[gridId][chunk] = new Dictionary<int, Color>();

                foreach (var (region, nodes) in regions)
                {
                    Regions[gridId][chunk].Add(region, nodes);
                    _regionColors[gridId][chunk][region] = new Color(robustRandom.NextFloat(), robustRandom.NextFloat(),
                        robustRandom.NextFloat(), 0.3f);
                }
            }
        }

        private void DrawRegions(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            var attachedEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (attachedEntity == null || !Regions.TryGetValue(attachedEntity.Transform.GridID, out var entityRegions))
            {
                return;
            }

            foreach (var (chunk, regions) in entityRegions)
            {
                foreach (var (region, nodes) in regions)
                {
                    foreach (var tile in nodes)
                    {
                        if (!viewport.Contains(tile)) continue;

                        var screenTile = _eyeManager.WorldToScreen(tile);
                        var box = new UIBox2(
                            screenTile.X - 15.0f,
                            screenTile.Y - 15.0f,
                            screenTile.X + 15.0f,
                            screenTile.Y + 15.0f);

                        screenHandle.DrawRect(box, _regionColors[attachedEntity.Transform.GridID][chunk][region]);
                    }
                }
            }
        }
        #endregion

        #region Pathfinder
        private void DrawAStarRoutes(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            foreach (var route in AStarRoutes)
            {
                // Draw box on each tile of route
                foreach (var position in route.Route)
                {
                    if (!viewport.Contains(position)) continue;
                    var screenTile = _eyeManager.WorldToScreen(position);
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

        private void DrawAStarNodes(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            foreach (var route in AStarRoutes)
            {
                var highestGScore = route.GScores.Values.Max();

                foreach (var (tile, score) in route.GScores)
                {
                    if ((route.Route.Contains(tile) && (Modes & PathfindingDebugMode.Route) != 0) ||
                        !viewport.Contains(tile))
                    {
                        continue;
                    }

                    var screenTile = _eyeManager.WorldToScreen(tile);
                    var box = new UIBox2(
                        screenTile.X - 15.0f,
                        screenTile.Y - 15.0f,
                        screenTile.X + 15.0f,
                        screenTile.Y + 15.0f);

                    screenHandle.DrawRect(box, new Color(
                        0.0f,
                        score / highestGScore,
                        1.0f - (score / highestGScore),
                        0.1f));
                }
            }
        }

        private void DrawJpsRoutes(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            foreach (var route in JpsRoutes)
            {
                // Draw box on each tile of route
                foreach (var position in route.Route)
                {
                    if (!viewport.Contains(position)) continue;
                    var screenTile = _eyeManager.WorldToScreen(position);
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

        private void DrawJpsNodes(DrawingHandleScreen screenHandle, Box2 viewport)
        {
            foreach (var route in JpsRoutes)
            {
                foreach (var tile in route.JumpNodes)
                {
                    if ((route.Route.Contains(tile) && (Modes & PathfindingDebugMode.Route) != 0) ||
                        !viewport.Contains(tile))
                    {
                        continue;
                    }

                    var screenTile = _eyeManager.WorldToScreen(tile);
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

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (Modes == 0)
            {
                return;
            }

            var screenHandle = args.ScreenHandle;
            screenHandle.UseShader(_shader);
            var viewport = _eyeManager.GetWorldViewport();

            if ((Modes & PathfindingDebugMode.Route) != 0)
            {
                DrawAStarRoutes(screenHandle, viewport);
                DrawJpsRoutes(screenHandle, viewport);
            }

            if ((Modes & PathfindingDebugMode.Nodes) != 0)
            {
                DrawAStarNodes(screenHandle, viewport);
                DrawJpsNodes(screenHandle, viewport);
            }

            if ((Modes & PathfindingDebugMode.Graph) != 0)
            {
                DrawGraph(screenHandle, viewport);
            }

            if ((Modes & PathfindingDebugMode.CachedRegions) != 0)
            {
                DrawCachedRegions(screenHandle, viewport);
            }

            if ((Modes & PathfindingDebugMode.Regions) != 0)
            {
                DrawRegions(screenHandle, viewport);
            }
        }
    }

    [Flags]
    public enum PathfindingDebugMode : byte
    {
        None = 0,
        Route = 1 << 0,
        Graph = 1 << 1,
        Nodes = 1 << 2,
        CachedRegions = 1 << 3,
        Regions = 1 << 4,
    }
#endif
}
