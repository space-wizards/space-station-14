using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Content.Shared.Atmos;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Enums;
using System;

namespace Content.Client.Atmos
{
    public class AtmosDebugOverlay : Overlay
    {
        private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public AtmosDebugOverlay()
        {
            IoCManager.InjectDependencies(this);

            _atmosDebugOverlaySystem = EntitySystem.Get<AtmosDebugOverlaySystem>();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

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

                for (var pass = 0; pass < 2; pass++)
                {
                    foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                    {
                        var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);
                        if (dataMaybeNull != null)
                        {
                            var data = (SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData) dataMaybeNull!;
                            if (pass == 0)
                            {
                                // -- Mole Count --
                                float total = 0;
                                switch (_atmosDebugOverlaySystem.CfgMode) {
                                    case AtmosDebugOverlayMode.TotalMoles:
                                        foreach (float f in data.Moles)
                                        {
                                            total += f;
                                        }
                                        break;
                                    case AtmosDebugOverlayMode.GasMoles:
                                        total = data.Moles[_atmosDebugOverlaySystem.CfgSpecificGas];
                                        break;
                                    case AtmosDebugOverlayMode.Temperature:
                                        total = data.Temperature;
                                        break;
                                }
                                var interp = ((total - _atmosDebugOverlaySystem.CfgBase) / _atmosDebugOverlaySystem.CfgScale);
                                Color res;
                                if (_atmosDebugOverlaySystem.CfgCBM)
                                {
                                    // Greyscale interpolation
                                    res = Color.InterpolateBetween(Color.Black, Color.White, interp);
                                }
                                else
                                {
                                    // Red-Green-Blue interpolation
                                    if (interp < 0.5f)
                                    {
                                        res = Color.InterpolateBetween(Color.Red, Color.Green, interp * 2);
                                    }
                                    else
                                    {
                                        res = Color.InterpolateBetween(Color.Green, Color.Blue, (interp - 0.5f) * 2);
                                    }
                                }
                                res = res.WithAlpha(0.75f);
                                drawHandle.DrawRect(Box2.FromDimensions(mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)), new Vector2(1, 1)), res);
                            }
                            else if (pass == 1)
                            {
                                // -- Blocked Directions --
                                void CheckAndShowBlockDir(AtmosDirection dir)
                                {
                                    if (data.BlockDirection.HasFlag(dir))
                                    {
                                        // Account for South being 0.
                                        var atmosAngle = dir.ToAngle() - Angle.FromDegrees(90);
                                        var atmosAngleOfs = atmosAngle.ToVec() * 0.45f;
                                        var atmosAngleOfsR90 = new Vector2(atmosAngleOfs.Y, -atmosAngleOfs.X);
                                        var tileCentre = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
                                        var basisA = mapGrid.LocalToWorld(tileCentre + atmosAngleOfs - atmosAngleOfsR90);
                                        var basisB = mapGrid.LocalToWorld(tileCentre + atmosAngleOfs + atmosAngleOfsR90);
                                        drawHandle.DrawLine(basisA, basisB, Color.Azure);
                                    }
                                }
                                CheckAndShowBlockDir(AtmosDirection.North);
                                CheckAndShowBlockDir(AtmosDirection.South);
                                CheckAndShowBlockDir(AtmosDirection.East);
                                CheckAndShowBlockDir(AtmosDirection.West);
                                // -- Pressure Direction --
                                if (data.PressureDirection != AtmosDirection.Invalid)
                                {
                                    // Account for South being 0.
                                    var atmosAngle = data.PressureDirection.ToAngle() - Angle.FromDegrees(90);
                                    var atmosAngleOfs = atmosAngle.ToVec() * 0.4f;
                                    var tileCentre = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
                                    var basisA = mapGrid.LocalToWorld(tileCentre);
                                    var basisB = mapGrid.LocalToWorld(tileCentre + atmosAngleOfs);
                                    drawHandle.DrawLine(basisA, basisB, Color.Blue);
                                }
                                // -- Excited Groups --
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
