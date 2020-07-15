using System;
using System.Collections.Generic;
using Content.Client.Utility;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Atmos
{
    public class GasOverlay : Overlay
    {
        // TODO ATMOS make animated overlays work

        private float _timer = 0f;

        private readonly Dictionary<string, (Texture, RSI)> _animated = new Dictionary<string, (Texture, RSI)>();
        private Dictionary<GridId, Dictionary<MapIndices, List<string>>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, List<string>>>();

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _timer += args.DeltaSeconds;
        }

        public GasOverlay() : base(nameof(GasOverlay))
        {
            IoCManager.InjectDependencies(this);

            var gridTwoTest = new Dictionary<MapIndices, List<string>>();

            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    gridTwoTest[new MapIndices(x, y)] = new List<string>()
                    {
                        "/Textures/Mobs/Ghosts/ghost_human.rsi/icon.png"
                    };
                }
            }

            _overlay[new GridId(2)] = gridTwoTest;
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            var drawHandle = (DrawingHandleWorld) handle;

            var mapId = _eyeManager.CurrentMap;
            var eye = _eyeManager.CurrentEye;

            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / (float) EyeManager.PixelsPerMeter * eye.Zoom);

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_overlay.TryGetValue(mapGrid.Index, out var tiles) || tiles == null || tiles.Count == 0) continue;

                foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                {
                    if (!tiles.TryGetValue(tile.GridIndices, out var overlays) || overlays == null || overlays.Count == 0) continue;

                    foreach (var overlayPath in overlays)
                    {
                        var texture = _resourceCache.GetTexture(overlayPath);

                        drawHandle.DrawTexture(texture, new Vector2(tile.X, tile.Y));
                    }
                }
            }
        }
    }
}
