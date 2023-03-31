using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Atmos.Overlays
{
    public sealed class AtmosDebugOverlay : Overlay
    {
        private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        internal AtmosDebugOverlay(AtmosDebugOverlaySystem system)
        {
            IoCManager.InjectDependencies(this);

            _atmosDebugOverlaySystem = system;
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
                if (!_atmosDebugOverlaySystem.HasData(mapGrid.Owner) ||
                    !_entManager.TryGetComponent<TransformComponent>(mapGrid.Owner, out var xform))
                    continue;

                drawHandle.SetTransform(xform.WorldMatrix);

                for (var pass = 0; pass < 2; pass++)
                {
                    foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                    {
                        var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Owner, tile.GridIndices);
                        if (dataMaybeNull != null)
                        {
                            var data = (SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData) dataMaybeNull;
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
                                        res = Color.InterpolateBetween(Color.Red, Color.LimeGreen, interp * 2);
                                    }
                                    else
                                    {
                                        res = Color.InterpolateBetween(Color.LimeGreen, Color.Blue, (interp - 0.5f) * 2);
                                    }
                                }
                                res = res.WithAlpha(0.75f);
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
                                        drawHandle.DrawLine(basisA, basisB, Color.Azure);
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
                                    // Account for South being 0.
                                    var atmosAngle = d.ToAngle() - Angle.FromDegrees(90);
                                    var atmosAngleOfs = atmosAngle.ToVec() * 0.4f;
                                    var tileCentre = new Vector2(t.X + 0.5f, t.Y + 0.5f);
                                    var basisA = tileCentre;
                                    var basisB = tileCentre + atmosAngleOfs;
                                    handle.DrawLine(basisA, basisB, color);
                                }

                                // -- Pressure Direction --
                                if (data.PressureDirection != AtmosDirection.Invalid)
                                {
                                    DrawPressureDirection(drawHandle, data.PressureDirection, tile, Color.Blue);
                                }
                                else if (data.LastPressureDirection != AtmosDirection.Invalid)
                                {
                                    DrawPressureDirection(drawHandle, data.LastPressureDirection, tile, Color.LightGray);
                                }

                                var tilePos = new Vector2(tile.X, tile.Y);

                                // -- Excited Groups --
                                if (data.InExcitedGroup != 0)
                                {
                                    var basisA = tilePos;
                                    var basisB = tilePos + new Vector2(1.0f, 1.0f);
                                    var basisC = tilePos + new Vector2(0.0f, 1.0f);
                                    var basisD = tilePos + new Vector2(1.0f, 0.0f);
                                    var color = Color.White // Use first three nibbles for an unique color... Good enough?
                                        .WithRed(   data.InExcitedGroup & 0x000F)
                                        .WithGreen((data.InExcitedGroup & 0x00F0) >>4)
                                        .WithBlue( (data.InExcitedGroup & 0x0F00) >>8);
                                    drawHandle.DrawLine(basisA, basisB, color);
                                    drawHandle.DrawLine(basisC, basisD, color);
                                }

                                // -- Space Tiles --
                                if (data.IsSpace)
                                {
                                    drawHandle.DrawCircle(tilePos + Vector2.One/2, 0.125f, Color.Orange);
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
