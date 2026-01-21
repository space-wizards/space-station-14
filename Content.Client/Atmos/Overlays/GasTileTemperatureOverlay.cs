using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Client.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
namespace Content.Client.Atmos.Overlays;

public sealed class GasTileTemperatureOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = false;

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;

    private IRenderTexture? _overlayTarget;

    private readonly Color[] _colorCache = new Color[256];

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GasTileTemperatureOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();

        for (int i = 0; i <= 255; i++)
        {
            _colorCache[i] = CalculateColor((byte)i);
        }
    }

    private static Color CalculateColor(byte byteTemp)
    {
        const float MinInput = 0f;
        const float MaxInput = 1000f;
        const float Resolution = 250f;

        // Temp resolution, calculates how many degrees is one increment in byteTemp
        const float tempResolution = (MaxInput - MinInput) / Resolution;

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

        float tempK = byteTemp * tempResolution;

        // Neutral Zone Check (0C to 50C)
        // If between 273.15K and 323.15K, it's transparent.
        if (tempK >= waterFreezeK && tempK < heatStartK)
        {
            return Color.Transparent;
        }

        Color resultingColor;

        if (tempK < deepFreezeK)
        {
            resultingColor = Color.FromHex("#330066");
        }
        else if (tempK < freezeStartK)
        {
            // Interpolate Deep Purple -> Blue
            // Range: 123.15 to 223.15 (Span: 100)
            resultingColor = Color.InterpolateBetween(
                Color.FromHex("#330066"),
                Color.Blue,
                (tempK - deepFreezeK) * 0.01f);
        }
        else if (tempK < waterFreezeK)
        {
            // Interpolate Blue -> Transparent
            // Range: 223.15 to 273.15 (Span: 50)
            resultingColor = Color.InterpolateBetween(
                Color.Blue,
                Color.Transparent,
                (tempK - freezeStartK) * 0.02f);
        }
        else if (tempK < waterBoilK)
        {
            // Interpolate Transparent -> Yellow
            // Range: 323.15 to 373.15 (Span: 50)
            resultingColor = Color.InterpolateBetween(
                Color.Transparent,
                Color.Yellow,
                (tempK - heatStartK) * 0.02f);
        }
        else if (tempK < superHeatK)
        {
            // Interpolate Yellow -> Red
            // Range: 373.15 to 573.15 (Span: 200)
            resultingColor = Color.InterpolateBetween(
                Color.Yellow,
                Color.Red,
                (tempK - waterBoilK) * 0.005f);
        }
        else
        {
            resultingColor = Color.DarkRed;
        }

        resultingColor.A = 0.7f;
        return resultingColor;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace) return false;

        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();
        if (_gasTileOverlay == null) return false;

        var target = args.Viewport.RenderTarget;

        if (_overlayTarget?.Texture.Size != target.Size)
        {
            _overlayTarget?.Dispose();
            _overlayTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileTemperatureOverlay));
        }

        var drawHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;
        var worldAABB = args.WorldAABB;
        var mapId = args.MapId;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();


        bool anyGasDrawn = false;

        drawHandle.RenderInRenderTarget(_overlayTarget,
            () =>
            {
                List<Entity<MapGridComponent>> grids = new();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);

                foreach (var grid in grids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp)) continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt)) continue;

                    var uvToUi = Matrix3Helpers.CreateScale(_overlayTarget.Size.X, -_overlayTarget.Size.Y);
                    var uvToGridEnt = uvToUi * viewportLocalToGridEnt;

                    drawHandle.SetTransform(gridEntToViewportLocal);

                    var floatBounds = worldToViewportLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);
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
                            if (!localBounds.Contains(tilePosition)) continue;

                            Color gasColor = _colorCache[tileGas.TemperatureQuantization];

                            if (gasColor.A <= 0f) continue;

                            anyGasDrawn = true;

                            drawHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + new Vector2(0.5f, 0.5f), grid.Comp.TileSizeVector),
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
            _overlayTarget?.Dispose();
            _overlayTarget = null;
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_overlayTarget is null)
            return;

        args.WorldHandle.DrawTextureRect(_overlayTarget.Texture, args.WorldBounds);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _overlayTarget?.Dispose();
        _overlayTarget = null;
        base.DisposeBehavior();
    }
}
