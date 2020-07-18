using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Utility;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Atmos
{
    public class GasTileOverlay : Overlay
    {
        private GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public GasTileOverlay() : base(nameof(GasTileOverlay))
        {
            IoCManager.InjectDependencies(this);

            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
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
                foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                {
                    foreach (var (texture, opacity) in _gasTileOverlaySystem.GetOverlays(mapGrid.Index, tile.GridIndices))
                    {
                        drawHandle.DrawTexture(texture, new Vector2(tile.X, tile.Y), Color.White.WithAlpha(opacity));
                    }
                }
            }
        }
    }
}
