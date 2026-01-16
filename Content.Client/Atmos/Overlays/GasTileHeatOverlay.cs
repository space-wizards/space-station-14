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
    // We can't resolve this immediately, because it's an entitysystem, but we will attempt to resolve and cache this
    // once we begin to draw.
    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;

    private IRenderTexture? _heatTarget;
    private IRenderTexture? _heatBlurTarget;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
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
        _shader.SetParameter("strength_scale", reducedMotion ? 0.5f : 1f);
        _shader.SetParameter("speed_scale", reducedMotion ? 0.25f : 1f);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        // If we haven't resolved this yet, give it a try or bail
        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();

        if (_gasTileOverlay == null)
            return false;

        var target = args.Viewport.RenderTarget;

        // Probably the resolution of the game window changed, remake the textures.
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
        args.WorldHandle.RenderInRenderTarget(_heatTarget,
            () =>
            {
                List<Entity<MapGridComponent>> grids = new();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref grids);
                foreach (var grid in grids)
                {
                    if (!overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        continue;

                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    if (!Matrix3x2.Invert(gridEntToViewportLocal, out var viewportLocalToGridEnt))
                        continue;

                    var uvToUi = Matrix3Helpers.CreateScale(_heatTarget.Size.X, -_heatTarget.Size.Y);
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
                    var floatBounds = worldToViewportLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);
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
                            // --->
                            // Check and make sure the tile is within the viewport/screen
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;

                            // Get the distortion strength from the temperature and bail if it's not hot enough
                            var strength = _gasTileOverlay.GetHeatDistortionStrength(tileGas.Temperature);
                            if (strength <= 0f)
                                continue;

                            anyDistortion = true;
                            // Encode the strength in the red channel, then 1.0 alpha if it's an active tile.
                            // BlurRenderTarget will then apply a blur around the edge, but we don't want it to bleed
                            // past the tile.
                            // So we use this alpha channel to chop the lower alpha values off in the shader to fit a
                            // fit mask back into the tile.
                            worldHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + new Vector2(0.5f, 0.5f), grid.Comp.TileSizeVector),
                                new Color(strength,0f, 0f, strength > 0f ? 1.0f : 0f));
                        }
                    }
                }
            },
            // This clears the buffer to all zero first...
            new Color(0, 0, 0, 0));

        // no distortion, no need to render
        if (!anyDistortion)
        {
            // Return the draw handle to normal settings
            args.WorldHandle.UseShader(null);
            args.WorldHandle.SetTransform(Matrix3x2.Identity);
            return false;
        }

        // Clear to draw
        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || _heatTarget is null || _heatBlurTarget is null)
            return;

        // Blur to soften the edges of the distortion. the lower parts of the alpha channel need to get cut off in the
        // distortion shader to keep them in tile bounds.
        _clyde.BlurRenderTarget(args.Viewport, _heatTarget, _heatBlurTarget, args.Viewport.Eye!, 14f);

        // Set up and render the distortion
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawTextureRect(_heatTarget.Texture, args.WorldBounds);

        // Return the draw handle to normal settings
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
