using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Atmos.Overlays
{
    public sealed class AtmosDebugWorldspaceOverlay : Overlay
    {
        private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public AtmosDebugWorldspaceOverlay()
        {
            IoCManager.InjectDependencies(this);

            _atmosDebugOverlaySystem = EntitySystem.Get<AtmosDebugOverlaySystem>();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

            var mapId = args.Viewport.Eye!.Position.MapId;
            var worldBounds = args.WorldBounds;

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

                drawHandle.SetTransform(mapGrid.WorldMatrix);

                for (var pass = 0; pass < 2; pass++)
                {
                    foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                    {
                        var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);
                        if (dataMaybeNull != null)
                        {
                            var data = (SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData) dataMaybeNull!;

                            if (pass == 0 && _atmosDebugOverlaySystem.CfgMode == AtmosDebugOverlayMode.Everything)
                            {
                                var topValueToShow = _atmosDebugOverlaySystem.CfgScale + _atmosDebugOverlaySystem.CfgBase;
                                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                                {
                                    if (data.Moles[i] > topValueToShow)
                                    {
                                        topValueToShow = data.Moles[i];
                                    }
                                }

                                // all the lines
                                var lineZeroX = tile.X + 0.55f;
                                var linesBottomY = tile.Y + 0.15f;
                                var linesMaxLen = 0.4f;

                                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                                {
                                    if (data.Moles[i] == 0f) continue;
                                    var lineBottom = new Vector2(lineZeroX + (0.05f * i), linesBottomY);
                                    var interp = (float)
                                        (Math.Log10(data.Moles[i] - _atmosDebugOverlaySystem.CfgBase) /
                                                  Math.Log10(topValueToShow - _atmosDebugOverlaySystem.CfgBase));
                                    if (interp < 0f) interp = 0.1f;
                                    var lineTop = lineBottom + new Vector2(0, linesMaxLen * interp);
                                    drawHandle.DrawLine(lineBottom, lineTop, Atmospherics.GasColors[(Gas) i]);
                                }
                            }
                            else if (pass == 0)
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
                                        res = Color.InterpolateBetween(Color.Red, Color.LimeGreen, interp * 2);
                                    }
                                    else
                                    {
                                        res = Color.InterpolateBetween(Color.LimeGreen, Color.Blue, (interp - 0.5f) * 2);
                                    }
                                }
                                res = res.WithAlpha(0.2f);
                                drawHandle.DrawRect(Box2.FromDimensions(new Vector2(tile.X, tile.Y), new Vector2(1, 1)), res);
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
                                        var basisA = tileCentre + atmosAngleOfs - atmosAngleOfsR90;
                                        var basisB = tileCentre + atmosAngleOfs + atmosAngleOfsR90;
                                        drawHandle.DrawLine(basisA, basisB, new Color(96,32,32));
                                    }
                                }
                                CheckAndShowBlockDir(AtmosDirection.North);
                                CheckAndShowBlockDir(AtmosDirection.South);
                                CheckAndShowBlockDir(AtmosDirection.East);
                                CheckAndShowBlockDir(AtmosDirection.West);

                                void DrawPressureDirection(
                                    DrawingHandleWorld handle,
                                    AtmosDirection d,
                                    TileRef t,
                                    Color color)
                                {
                                    var arrowFinLength = 0.3f;

                                    // Account for South being 0.
                                    var atmosAngle = d.ToAngle() - Angle.FromDegrees(90);

                                    var atmosAngleOfs = atmosAngle.ToVec();
                                    var arrowTip = new Vector2(t.X + 0.25f, t.Y + 0.75f) + atmosAngleOfs * 0.25f;
                                    var arrowFinRightSide = arrowTip - (Angle.FromDegrees(25).RotateVec(atmosAngleOfs)
                                        * arrowFinLength);
                                    var arrowFinLeftSide = arrowTip - (Angle.FromDegrees(-25).RotateVec(atmosAngleOfs)
                                        * arrowFinLength);

                                    handle.DrawLine(arrowFinRightSide, arrowTip, color);
                                    handle.DrawLine(arrowFinLeftSide, arrowTip, color);
                                }

                                // -- Pressure Direction --
                                if (data.PressureDirection != AtmosDirection.Invalid)
                                {
                                    DrawPressureDirection(drawHandle, data.PressureDirection, tile, Color.Pink);
                                }
                                else if (data.LastPressureDirection != AtmosDirection.Invalid)
                                {
                                    DrawPressureDirection(drawHandle, data.LastPressureDirection, tile, Color.Gray);
                                }
                            }
                        }
                    }
                }
            }

            drawHandle.SetTransform(Matrix3.Identity);
        }
    }
}
