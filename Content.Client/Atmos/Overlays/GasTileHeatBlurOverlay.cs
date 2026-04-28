using Content.Client.Atmos.EntitySystems;
using Content.Client.Graphics;
using Content.Client.Resources;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using Color = Robust.Shared.Maths.Color;
using Texture = Robust.Client.Graphics.Texture;

namespace Content.Client.Atmos.Overlays;

/// <summary>
///     Overlay responsible for rendering heat distortion shader.
/// </summary>
public sealed class GasTileHeatBlurOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = true;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> HeatOverlayShader = "HeatBlur";

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly SharedTransformSystem _xformSys;
    private readonly ShaderInstance _shader;

    private readonly Texture _noiseTexture;
    private readonly Texture _heatGradientTexture;
    private List<Entity<MapGridComponent>> _intersectingGrids = new();
    private readonly OverlayResourceCache<CachedResources> _resources = new();

    // Overlay settings
    private const float
        ShaderSpilling = 2.5f; // for example 4f - spills shader one tile from hotspot, 2.5f - spills it half tile

    private const float ShaderStrength = 0.04f; // Makes waves stronger
    private const float ShaderScale = 1f; // Makes more waves
    private const float ShaderSpeed = 0.4f; // Makes waves run faster

    // Overlay settings for reduced motion setting
    private const float ShaderStrengthForReducedMotion = 0.01f;
    private const float ShaderScaleReducedMotion = 0.5f;
    private const float ShaderSpeedReducedMotion = 0.25f;

    private const int MinDistortionTemp = 300; // Distortion starts to show up at this temperature in Kelvins
    private const int MaxDistortionTemp = 2000; // Maximum distortion strength at this temperature in Kelvins

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public GasTileHeatBlurOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();

        _noiseTexture = _resourceCache.GetTexture("/Textures/Effects/HeatBlur/perlin_noise.png");
        _heatGradientTexture = _resourceCache.GetTexture("/Textures/Effects/HeatBlur/soft_circle.png");

        _shader = _proto.Index(HeatOverlayShader).InstanceUnique();
        _configManager.OnValueChanged(CCVars.ReducedMotion, SetReducedMotion, invokeImmediately: true);
    }

    private void SetReducedMotion(bool reducedMotion)
    {
        _shader.SetParameter("strength_scale", reducedMotion ? ShaderStrengthForReducedMotion : ShaderStrength);
        _shader.SetParameter("spatial_scale", reducedMotion ? ShaderScaleReducedMotion : ShaderScale);
        _shader.SetParameter("speed_scale", reducedMotion ? ShaderSpeedReducedMotion : ShaderSpeed);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        var target = args.Viewport.RenderTarget;

        // Probably the resolution of the game window changed, remake the textures.
        if (res.HeatTarget?.Texture.Size != target.Size)
        {
            res.HeatTarget?.Dispose();
            res.HeatTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileHeatBlurOverlaySystem));
        }

        if (res.HeatBlurTarget?.Texture.Size != target.Size)
        {
            res.HeatBlurTarget?.Dispose();
            res.HeatBlurTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: $"{nameof(GasTileHeatBlurOverlaySystem)}-blur");
        }

        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        args.WorldHandle.UseShader(_proto.Index(UnshadedShader).Instance());

        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        // If there is no distortion after checking all visible tiles, we can bail early
        var anyDistortion = false;

        // We're rendering in the context of the heat target texture, which will encode data as to where and how strong
        // the heat distortion will be
        args.WorldHandle.RenderInRenderTarget(res.HeatTarget,
            () =>
            {
                _intersectingGrids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref _intersectingGrids);
                foreach (var grid in _intersectingGrids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt))
                        continue;

                    var uvToUi = Matrix3Helpers.CreateScale(res.HeatTarget.Size.X, -res.HeatTarget.Size.Y);
                    var uvToGridEnt = uvToUi * viewportLocalToGridEnt;

                    // Because we want the actual distortion to be calculated based on the grid coordinates*, we need
                    // to pass a matrix transformation to go from the viewport coordinates to grid coordinates.
                    //   * (why? because otherwise the effect would shimmer like crazy as you moved around, think
                    //      moving a piece of warped glass above a picture instead of placing the warped glass on the
                    //      paper and moving them together)
                    _shader.SetParameter("grid_ent_from_viewport_local", uvToGridEnt);

                    // Draw commands (like DrawRect) will be using grid coordinates from here
                    worldHandle.SetTransform(gridEntToViewportLocal);

                    // We only care about tiles that fit in these bounds
                    var worldToGridLocal = _xformSys.GetInvWorldMatrix(grid.Owner);
                    var floatBounds = worldToGridLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);

                    var localBounds = new Box2i(
                        (int)MathF.Floor(floatBounds.Left),
                        (int)MathF.Floor(floatBounds.Bottom),
                        (int)MathF.Ceiling(floatBounds.Right),
                        (int)MathF.Ceiling(floatBounds.Top));

                    // for each tile and its gas --->
                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);

                        while (enumerator.MoveNext(out var tileGas))
                        {
                            // Check and make sure the tile is within the viewport/screen
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;

                            // Get the distortion strength from the temperature and bail if it's not hot enough
                            var strength = GetHeatDistortionStrength(tileGas.ByteGasTemperature);
                            if (strength <= 0f)
                                continue;

                            anyDistortion = true;

                            // Encode the strength in the red channel
                            // alpha set to 1 as tile is active
                            worldHandle.DrawTextureRect(
                                _heatGradientTexture,
                                Box2.CenteredAround(tilePosition + grid.Comp.TileSizeHalfVector,
                                    grid.Comp.TileSizeVector * ShaderSpilling),
                                new Color(strength, 0f, 0f));
                        }
                    }
                }
            },
            // This clears the buffer to all zero first...
            new Color(0, 0, 0, 0));

        // no distortion, no need to render
        if (!anyDistortion)
        {
            args.WorldHandle.UseShader(null);
            args.WorldHandle.SetTransform(Matrix3x2.Identity);
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (ScreenTexture is null || res.HeatTarget is null || res.HeatBlurTarget is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("NOISE_TEXTURE", _noiseTexture);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawTextureRect(res.HeatTarget.Texture, args.WorldBounds);

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        _configManager.UnsubValueChanged(CCVars.ReducedMotion, SetReducedMotion);
        base.DisposeBehavior();
    }

    /// <summary>
    /// Gets the strength of the heat distortion effect based on the temperature of the tile.
    /// The strength is a value between 0 and 1, where 0 means no distortion and 1 means maximum distortion.
    /// </summary>
    /// <param name="temp">The temperature of the tile.</param>
    /// <returns>The strength of the heat distortion effect.</returns>
    /// <seealso cref="ThermalByte"/>
    private static float GetHeatDistortionStrength(ThermalByte temp)
    {
        if (!temp.TryGetTemperature(out var kelvinTemp))
        {
            return 0f;
        }

        var strength = (kelvinTemp - MinDistortionTemp) / (MaxDistortionTemp - MinDistortionTemp);

        return MathHelper.Clamp01(strength);
    }

    internal sealed class CachedResources : IDisposable
    {
        public IRenderTexture? HeatTarget;
        public IRenderTexture? HeatBlurTarget;

        public void Dispose()
        {
            HeatTarget?.Dispose();
            HeatBlurTarget?.Dispose();
        }
    }
}
