using Content.Client.GameObjects.EntitySystems;
using Robust.Shared.Enums;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Atmos
{
    public class GasTileOverlay : Overlay
    {
        private readonly GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

        public GasTileOverlay()
        {
            IoCManager.InjectDependencies(this);

            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

            var mapId = _eyeManager.CurrentMap;
            var eye = _eyeManager.CurrentEye;

            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / (float) EyeManager.PixelsPerMeter * eye.Zoom);

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                var gridBounds = new Box2(mapGrid.WorldToLocal(worldBounds.BottomLeft), mapGrid.WorldToLocal(worldBounds.TopRight));

                foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                {
                    foreach (var (texture, color) in _gasTileOverlaySystem.GetOverlays(mapGrid.Index, tile.GridIndices))
                    {
                        drawHandle.DrawTexture(texture, mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)), color);
                    }
                }
            }
        }
    }
}
