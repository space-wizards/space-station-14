using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Atmos.Overlays;

public sealed class GasTileHeatOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = true;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> HeatOverlayShader = "Heat";

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;

    private IRenderTexture? _heatTarget;
    private IRenderTexture? _heatBlurTarget;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    private readonly ShaderInstance _shader;

    public GasTileHeatOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();
        _shader = _proto.Index(HeatOverlayShader).InstanceUnique();
        _configManager.OnValueChanged(CCVars.ReducedMotion, SetReducedMotion, invokeImmediately: true);
    }

    private void SetReducedMotion(bool reducedMotion)
    {
        // These can remain if you want to keep distortion capabilities, 
        // otherwise they are unused by the simple color shader.
        _shader.SetParameter("strength_scale", reducedMotion ? 0.5f : 1f);
        _shader.SetParameter("speed_scale", reducedMotion ? 0.25f : 1f);
    }

    private Color GetGasColor(float tempK)
    {
        float tempC = tempK - 273.15f;

        // 1. Extreme Cold (Below -150C) -> Solid Purple
        if (tempC < -150f)
            return Color.FromHex("#330066");

        // 2. Deep Freeze (-150C to -50C) -> Purple to Blue
        if (tempC < -50f)
        {
            // Range size: 100 degrees
            // t = 0.0 at -150, t = 1.0 at -50
            float t = (tempC + 150f) / 100f;
            return Color.InterpolateBetween(Color.FromHex("#330066"), Color.Blue, t);
        }

        // 3. Freezing to Safe (-50C to 0C) -> Blue to Transparent
        if (tempC < 0f)
        {
            // Range size: 50 degrees
            // t = 0.0 at -50 (Blue), t = 1.0 at 0 (Transparent)
            float t = (tempC + 50f) / 50f;
            return Color.InterpolateBetween(Color.Blue, Color.Transparent, t);
        }

        // 4. Safe Zone (0C to 50C) -> Fully Transparent
        if (tempC < 50f)
        {
            return Color.Transparent;
        }

        // 5. Warming Up (50C to 100C) -> Transparent to Yellow
        if (tempC < 100f)
        {
            // Range size: 50 degrees
            // t = 0.0 at 50, t = 1.0 at 100
            float t = (tempC - 50f) / 50f;
            return Color.InterpolateBetween(Color.Transparent, Color.Yellow, t);
        }

        // 6. Danger Heat (100C to 300C) -> Yellow to Red
        if (tempC < 300f)
        {
            // Range size: 200 degrees
            // t = 0.0 at 100, t = 1.0 at 300
            float t = (tempC - 100f) / 200f;
            return Color.InterpolateBetween(Color.Yellow, Color.Red, t);
        }

        // 7. Extreme Heat (Over 300C) -> Dark Red
        return Color.DarkRed;
    }


    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();

        if (_gasTileOverlay == null)
            return false;

        var target = args.Viewport.RenderTarget;

        // Resize render targets if window size changes
        if (_heatTarget?.Texture.Size != target.Size)
        {
            _heatTarget?.Dispose();
            _heatTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileHeatOverlay));
        }


        if (_heatBlurTarget?.Texture.Size != target.Size)
        {
            _heatBlurTarget?.Dispose();
            _heatBlurTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: $"{nameof(GasTileHeatOverlay)}-blur");
        }

        // --- FIX: COPY VARIABLES LOCALLY FOR LAMBDA USE ---
        // We cannot use 'args' inside the lambda because it is an 'in' parameter.
        // We copy the handles and bounds to local variables here.
        var drawHandle = args.WorldHandle;      // Renamed to drawHandle
        var worldBounds = args.WorldBounds;
        var worldAABB = args.WorldAABB;
        var mapId = args.MapId;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();
        // --------------------------------------------------

        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        // Use unshaded shader for drawing the squares
        drawHandle.UseShader(_proto.Index(UnshadedShader).Instance());

        var anyDistortion = false;

        // Render the heat tiles into the texture
        drawHandle.RenderInRenderTarget(_heatTarget,
            () =>
            {
                // Clear the texture to TRANSPARENT first
                // FIX: Use local 'drawHandle' and 'worldBounds'
                drawHandle.DrawRect(worldBounds, Color.Transparent);

                List<Entity<MapGridComponent>> grids = new();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);

                foreach (var grid in grids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp)) continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt)) continue;

                    var uvToUi = Matrix3Helpers.CreateScale(_heatTarget.Size.X, -_heatTarget.Size.Y);
                    var uvToGridEnt = uvToUi * viewportLocalToGridEnt;

                    _shader.SetParameter("grid_ent_from_viewport_local", uvToGridEnt);

                    // FIX: Use local 'drawHandle'
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

                            //if (tileGas.Temperature <= 2.7f) continue;// || (273.15f < tileGas.Temperature && tileGas.Temperature < 323.15f)

                            anyDistortion = true;

                            Color gasColor = GetGasColor(tileGas.Temperature);

                           // tileGas.Opacity
                            // FIX: Use local 'drawHandle'
                            drawHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + new Vector2(0.5f, 0.5f), grid.Comp.TileSizeVector),
                                gasColor
                            );
                        }
                    }
                }
            },
            new Color(0, 0, 0, 0));

        if (!anyDistortion)
        {
            // FIX: Use local 'drawHandle'
            drawHandle.UseShader(null);
            drawHandle.SetTransform(Matrix3x2.Identity);
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || _heatTarget is null || _heatBlurTarget is null)
            return;

        // 1. Blur the blocks to create a smooth gradient
        _clyde.BlurRenderTarget(args.Viewport, _heatTarget, _heatBlurTarget, args.Viewport.Eye!, 5f); // 5f is Blur Radius

        // 2. Apply the Shader
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        args.WorldHandle.UseShader(_shader);

        // 3. IMPORTANT: Draw the BLURRED target, not the sharp one
        args.WorldHandle.DrawTextureRect(_heatBlurTarget.Texture, args.WorldBounds);

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _heatTarget = null;
        _heatBlurTarget = null;
        _configManager.UnsubValueChanged(CCVars.ReducedMotion, SetReducedMotion);
        base.DisposeBehavior();
    }
}
