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

            // IF YOU ARE ABOUT TO INTRODUCE CHUNKING OR SOME OTHER OPTIMIZATION INTO THIS CODE:
            //  -- THINK! --
            // 1. "Is this going to make a critical atmos debugging tool harder to debug itself?"
            // 2. "Is this going to do anything that could cause the atmos debugging tool to use resources, server-side or client-side, when nobody's using it?"
            // 3. "Is this going to make it harder for atmos programmers to add data that may not be chunk-friendly into the atmos debugger?"
            // Nanotrasen needs YOU! to avoid premature optimization in critical debugging tools - 20kdc

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_atmosDebugOverlaySystem.HasData(mapGrid.Index))
                    continue;

                var gridBounds = new Box2(mapGrid.WorldToLocal(worldBounds.BottomLeft), mapGrid.WorldToLocal(worldBounds.TopRight));

                for (var pass = 0; pass < 3; pass++)
                {
                    foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                    {
                        var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);
                        if (dataMaybeNull != null)
                        {
                            var data = (SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData) dataMaybeNull!;
                            if (pass == 0)
                            {
                                float total = 0;
                                foreach (float f in data.Moles)
                                {
                                    total += f;
                                }
                                var interp = total / (Atmospherics.MolesCellStandard * 2);
                                var res = Color.InterpolateBetween(Color.Red, Color.Green, interp).WithAlpha(0.75f);
                                drawHandle.DrawRect(Box2.FromDimensions(mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)), new Vector2(1, 1)), res);
                            }
                            else if (pass == 1)
                            {
                                if (data.PressureDirection != AtmosDirection.Invalid)
                                {
                                    var atmosAngle = data.PressureDirection.ToAngle();
                                    var atmosAngleOfs = atmosAngle.ToVec() * 0.4f;
                                    var tileCentre = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
                                    var basisA = mapGrid.LocalToWorld(tileCentre);
                                    var basisB = mapGrid.LocalToWorld(tileCentre + atmosAngleOfs);
                                    drawHandle.DrawLine(basisA, basisB, Color.Blue);
                                }
                            }
                            else if (pass == 2)
                            {
                                if (data.InExcitedGroup)
                                {
                                    var tilePos = new Vector2(tile.X, tile.Y);
                                    var basisA = mapGrid.LocalToWorld(tilePos);
                                    var basisB = mapGrid.LocalToWorld(tilePos + new Vector2(1.0f, 1.0f));
                                    var basisC = mapGrid.LocalToWorld(tilePos + new Vector2(0.0f, 1.0f));
                                    var basisD = mapGrid.LocalToWorld(tilePos + new Vector2(1.0f, 0.0f));
                                    drawHandle.DrawLine(basisA, basisB, Color.Cyan);
                                    drawHandle.DrawLine(basisC, basisD, Color.Cyan);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
