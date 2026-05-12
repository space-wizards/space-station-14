using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Atmos.Overlays;

/// <summary>
/// Renders a thermal heatmap overlay for gas tiles, used for equipment like thermal glasses.
/// /// </summary>
public sealed class GasTileDangerousTemperatureOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = false;

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;
    private EntityQuery<GasTileOverlayComponent> _overlayQuery;

    private IRenderTexture? _temperatureTarget;

    // Cache used to transform ThermalByte into Color for overlay
    private readonly Color[] _colorCache = new Color[256];

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GasTileDangerousTemperatureOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();

        _overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        for (byte i = 0; i <= ThermalByte.TempResolution; i++)
        {
            _colorCache[i] = PreCalculateColor(i);
        }

        _colorCache[ThermalByte.StateVacuum] = Color.Teal;
        _colorCache[ThermalByte.StateVacuum].A = 0.6f;
        _colorCache[ThermalByte.AtmosImpossible] = Color.Transparent;

#if DEBUG // This shouldn't happend so tell me if you see this LimeGreen on the screen
        _colorCache[ThermalByte.ReservedFuture0] = Color.LimeGreen;
        _colorCache[ThermalByte.ReservedFuture1] = Color.LimeGreen;
        _colorCache[ThermalByte.ReservedFuture2] = Color.LimeGreen;
#else
        _colorCache[ThermalByte.ReservedFuture0] = Color.Transparent;
        _colorCache[ThermalByte.ReservedFuture1] = Color.Transparent;
        _colorCache[ThermalByte.ReservedFuture2] = Color.Transparent;
#endif
    }


    /// <summary>
    /// Used for Calculating onscreen color from ThermalByte core value
    /// /// </summary>
    private static Color PreCalculateColor(byte byteTemp)
    {
        // Color Thresholds in Kelvin
        // -150 C
        const float deepFreezeK = 123.15f;
        // -50 C
        const float freezeStartK = 223.15f;
        // 0 C
        const float waterFreezeK = 273.15f;
        // 50 C
        const float heatStartK = 323.15f;
        // 100 C
        const float waterBoilK = 373.15f;
        // 300 C
        const float superHeatK = 573.15f;

        var tempK = byteTemp * ThermalByte.TempDegreeResolution;

        // Neutral Zone Check (0C to 50C)
        // If between 273.15K and 323.15K, it's transparent.
        if (tempK >= waterFreezeK && tempK < heatStartK)
        {
            return Color.Transparent;
        }

        Color resultingColor;

        switch (tempK)
        {
            case < deepFreezeK:
                resultingColor = Color.FromHex("#330066");
                resultingColor.A = 0.7f;
                break;
            case < freezeStartK:
                // Interpolate Deep Purple -> Blue
                // Range: 123.15 to 223.15 (Span: 100)
                resultingColor = Color.InterpolateBetween(
                    Color.FromHex("#330066"),
                    Color.Blue,
                    (tempK - deepFreezeK) * 0.01f);
                resultingColor.A = 0.6f;
                break;
            case < waterFreezeK:
                // Interpolate Blue -> Transparent
                // Range: 223.15 to 273.15 (Span: 50)

                resultingColor = Color.InterpolateBetween(
                    new Color(Color.Blue.R, Color.Blue.G, Color.Blue.B, 0.6f),
                    new Color(Color.Blue.R, Color.Blue.G, Color.Blue.B, 0.2f),
                    (tempK - freezeStartK) * 0.02f);
                break;
            case < waterBoilK:
                // Interpolate Transparent -> Yellow
                // Range: 323.15 to 373.15 (Span: 50)

                resultingColor = Color.InterpolateBetween(
                    new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, 0.2f),
                    new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, 0.6f),
                    (tempK - heatStartK) * 0.02f);
                break;
            case < superHeatK:
                // Interpolate Yellow -> Red
                // Range: 373.15 to 573.15 (Span: 200)
                resultingColor = Color.InterpolateBetween(
                    Color.Yellow,
                    Color.Red,
                    (tempK - waterBoilK) * 0.005f);
                resultingColor.A = 0.6f;
                break;
            default:
                resultingColor = Color.DarkRed;
                resultingColor.A = 0.7f;
                break;
        }

        return resultingColor;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();
        if (_gasTileOverlay == null)
            return false;

        var target = args.Viewport.RenderTarget;

        if (_temperatureTarget?.Texture.Size != target.Size)
        {
            _temperatureTarget?.Dispose();
            _temperatureTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileDangerousTemperatureOverlay));
        }

        var drawHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;
        var worldAABB = args.WorldAABB;
        var mapId = args.MapId;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        var anyGasDrawn = false;
        List<Entity<MapGridComponent>> grids = new();

        drawHandle.RenderInRenderTarget(_temperatureTarget,
            () =>
            {
                grids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);

                foreach (var grid in grids)
                {
                    if (!_overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        continue;

                    var gridTileSizeVec = grid.Comp.TileSizeVector;
                    var gridTileCenterVec = grid.Comp.TileSizeHalfVector;
                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    drawHandle.SetTransform(gridEntToViewportLocal);

                    var worldToGridLocal = _xformSys.GetInvWorldMatrix(grid.Owner);
                    var floatBounds = worldToGridLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);

                    var localBounds = new Box2i(
                        (int)MathF.Floor(floatBounds.Left),
                        (int)MathF.Floor(floatBounds.Bottom),
                        (int)MathF.Ceiling(floatBounds.Right),
                        (int)MathF.Ceiling(floatBounds.Top));

                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);
                        while (enumerator.MoveNext(out var tileGas))
                        {
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;

                            var gasColor = _colorCache[tileGas.ByteGasTemperature.Value];

                            if (gasColor.A <= 0f)
                                continue;

                            anyGasDrawn = true;

                            drawHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + gridTileCenterVec, gridTileSizeVec),
                                gasColor
                            );
                        }
                    }
                }
            },
            new Color(0, 0, 0, 0));

        drawHandle.SetTransform(Matrix3x2.Identity);

        if (!anyGasDrawn)
        {
            _temperatureTarget?.Dispose();
            _temperatureTarget = null;
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_temperatureTarget is null)
            return;

        args.WorldHandle.DrawTextureRect(_temperatureTarget.Texture, args.WorldBounds);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _temperatureTarget?.Dispose();
        _temperatureTarget = null;
        base.DisposeBehavior();
    }
}
