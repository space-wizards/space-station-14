using Content.Client.Atmos.EntitySystems;
using Content.Client.Resources;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Atmos.Overlays;

public sealed class AtmosDebugGraphOverlay : Overlay
{
    private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Font _font;
    private static readonly Color BlockedDirectionColor = new(96, 32, 32);

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public AtmosDebugGraphOverlay()
    {
        IoCManager.InjectDependencies(this);

        _atmosDebugOverlaySystem = EntitySystem.Get<AtmosDebugOverlaySystem>();
        _font = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 10);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if ((args.Space & OverlaySpace.WorldSpace) != 0)
        {
            WorldSpaceDraw(args);
        }
        else if ((args.Space & OverlaySpace.ScreenSpace) != 0)
        {
            ScreenSpaceDraw(args);
        }
    }

    private void WorldSpaceDraw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;

        var mapId = args.Viewport.Eye!.Position.MapId;
        var worldBounds = args.WorldBounds;

        foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            if (!_atmosDebugOverlaySystem.HasData(mapGrid.Index))
                continue;

            drawHandle.SetTransform(mapGrid.WorldMatrix);

            foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
            {
                var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);
                if (!dataMaybeNull.HasValue) continue;

                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.BlockDirections) != 0)
                    DrawBlockDirections(drawHandle, tile, dataMaybeNull.Value);
                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.GasMoles) != 0)
                    DrawGasMolesBarChart(drawHandle, tile, dataMaybeNull.Value);
                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.FlowDirections) != 0)
                    DrawFlowDirections(drawHandle, tile, dataMaybeNull.Value);
            }
        }

        drawHandle.SetTransform(Matrix3.Identity);
    }

    private void DrawGasMolesBarChart(DrawingHandleWorld drawHandle,
        TileRef tile,
        SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData data)
    {
        var topValueToShow = _atmosDebugOverlaySystem.CfgScale + _atmosDebugOverlaySystem.CfgBase;
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            if (data.Moles[i] > topValueToShow)
            {
                topValueToShow = data.Moles[i];
            }
        }

        {
            var lineZeroX = tile.X + 0.55f;
            var linesBottomY = tile.Y + 0.15f;
            const float linesMaxLen = 0.4f;

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                if (data.Moles[i] == 0f) continue;
                var lineBottom = new Vector2(lineZeroX + (0.05f * i), linesBottomY);
                var interp = (float)
                    (Math.Log10(data.Moles[i] - _atmosDebugOverlaySystem.CfgBase) /
                     Math.Log10(topValueToShow - _atmosDebugOverlaySystem.CfgBase));
                if (interp < 0f) interp = 0.1f;
                var lineTop = lineBottom + new Vector2(0, linesMaxLen * interp);
                drawHandle.DrawLine(lineBottom, lineTop, Atmospherics.GasDisplayColors[i]);
            }
        }
    }

    private static ValueTuple<Vector2, Vector2> TileFaceInDirection(TileRef tile, AtmosDirection dir)
    {
        // Account for South being 0.
        var atmosAngle = dir.ToAngle() - Angle.FromDegrees(90);
        var atmosAngleOfs = atmosAngle.ToVec() * 0.5f;
        var atmosAngleOfsR90 = new Vector2(atmosAngleOfs.Y, -atmosAngleOfs.X);
        var tileCentre = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
        var basisA = tileCentre + atmosAngleOfs - atmosAngleOfsR90;
        var basisB = tileCentre + atmosAngleOfs + atmosAngleOfsR90;
        return (basisA, basisB);
    }

    private static void DrawBlockDirections(DrawingHandleWorld drawHandle, TileRef tile,
        SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData data)
    {
        // -- Blocked Directions --
        void CheckAndShowBlockDir(AtmosDirection dir)
        {
            if ((data.BlockDirection & dir) == 0) return;
            var face = TileFaceInDirection(tile, dir);
            drawHandle.DrawLine(face.Item1, face.Item2, BlockedDirectionColor);
        }

        CheckAndShowBlockDir(AtmosDirection.North);
        CheckAndShowBlockDir(AtmosDirection.South);
        CheckAndShowBlockDir(AtmosDirection.East);
        CheckAndShowBlockDir(AtmosDirection.West);
    }

    private static void DrawFlowDirections(DrawingHandleWorld drawHandle, TileRef tile,
        SharedAtmosDebugOverlaySystem.AtmosDebugOverlayData data)
    {
        void DrawArrow(
                DrawingHandleWorld handle,
                AtmosDirection d,
                TileRef t,
                Color color)
        {
            // Account for South being 0.
            var atmosAngle = d.ToAngle() - Angle.FromDegrees(90);
            var atmosAngleOfs = atmosAngle.ToVec();

            const float arrowFinLength = 0.3f;

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
            DrawArrow(drawHandle, data.PressureDirection, tile, Color.Pink);
        }
        else if (data.LastPressureDirection != AtmosDirection.Invalid)
        {
            DrawArrow(drawHandle, data.LastPressureDirection, tile, Color.Gray);
        }
    }

    private void ScreenSpaceDraw(in OverlayDrawArgs args)
    {
        var mapId = args.Viewport.Eye!.Position.MapId;
        var worldBounds = args.WorldBounds;

        foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            if (!_atmosDebugOverlaySystem.HasData(mapGrid.Index))
                continue;

            foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
            {
                var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);

                if (!dataMaybeNull.HasValue) continue;
                var data = dataMaybeNull.Value;

                var tileCenterScreenCoords = _eyeManager.MapToScreen(mapGrid.GridTileToWorld(tile.GridIndices));
                var drawToScreenCoords = tileCenterScreenCoords.Position + new Vector2(-55f, 30f);

                const float lineHeight = 11f;

                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.TotalMoles) != 0)
                {
                    var totalMoles = 0f;
                    var overScale = false;

                    for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                    {
                        totalMoles += data.Moles[i];
                        if (data.Moles[i] > _atmosDebugOverlaySystem.CfgScale + _atmosDebugOverlaySystem.CfgBase)
                        {
                            overScale = true;
                        }
                    }

                    if (totalMoles != 0f)
                    {
                        args.ScreenHandle.DrawString(_font, drawToScreenCoords, totalMoles.ToString("0.## E-0"),
                            overScale ? Color.Red : Color.White);
                    }
                }

                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.Temperature) != 0)
                {
                    var tempColor = data.Temperature switch
                    {
                        > Atmospherics.T0C + 50 => Color.Red,
                        < Atmospherics.T0C => Color.Aqua,
                        _ => Color.White
                    };
                    args.ScreenHandle.DrawString(_font, drawToScreenCoords - new Vector2(0f, lineHeight),
                        (data.Temperature - Atmospherics.T0C).ToString("0.# C"), tempColor);
                }

                if ((_atmosDebugOverlaySystem.CfgMode & AtmosDebugShowMode.ExcitedGroups) != 0)
                {
                    if (data.InExcitedGroup || data.IsHotspot)
                    {
                        var line = (data.InExcitedGroup ? (data.IsHotspot ? "EXC HOT" : "EXC") : "HOT");
                        var color = data.IsHotspot ? Color.OrangeRed : Color.Yellow;
                        args.ScreenHandle.DrawString(_font, drawToScreenCoords - new Vector2(0f, lineHeight * 2), line,
                            color);
                    }
                }
            }
        }
    }
}
