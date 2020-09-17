using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Content.Shared.Atmos;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Atmos
{
    public class AtmosDebugOverlay : Overlay
    {
        private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public AtmosDebugOverlay() : base(nameof(AtmosDebugOverlay))
        {
            IoCManager.InjectDependencies(this);

            _atmosDebugOverlaySystem = EntitySystem.Get<AtmosDebugOverlaySystem>();
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace overlay)
        {
            var drawHandle = (DrawingHandleWorld) handle;

            var mapId = _eyeManager.CurrentMap;
            var eye = _eyeManager.CurrentEye;

            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / (float) EyeManager.PixelsPerMeter * eye.Zoom);

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_atmosDebugOverlaySystem.HasData(mapGrid.Index))
                    continue;

                var gridBounds = new Box2(mapGrid.WorldToLocal(worldBounds.BottomLeft), mapGrid.WorldToLocal(worldBounds.TopRight));
                
                foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                {
                    var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);
                    if (dataMaybeNull != null)
                    {
                        var data = (SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData) dataMaybeNull!;
                        float total = 0;
                        foreach (float f in data.Moles)
                        {
                            total += f;
                        }
                        var interp = total / (Atmospherics.MolesCellStandard * 2);
                        var res = Color.InterpolateBetween(Color.Red, Color.Green, interp).WithAlpha(0.75f);
                        drawHandle.DrawRect(Box2.FromDimensions(mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)), new Vector2(1, 1)), res);
                    }
                }
            }
        }
    }
}
