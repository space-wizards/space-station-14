using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Light;

/// <summary>
/// Applies ambient-occlusion to the viewport.
/// </summary>
public sealed class AmbientOcclusionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilEqualDrawShader = "StencilEqualDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private IRenderTexture? _aoTarget;
    private IRenderTexture? _aoBlurBuffer;

    // Couldn't figure out a way to avoid this so if you can then please do.
    private IRenderTexture? _aoStencilTarget;

    public AmbientOcclusionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        /*
         * tl;dr
         * - we draw a black square on each "ambient occlusion" entity.
         * - we blur this.
         * - We apply it to the viewport.
         *
         * We do this while ignoring lighting because it will wash out the actual effect.
         * In 3D ambient occlusion is more complicated due top having to calculate normals but in 2D
         * we don't have a concept of depth / corners necessarily.
         */

        var viewport = args.Viewport;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var color = Color.FromHex(_cfgManager.GetCVar(CCVars.AmbientOcclusionColor));
        var distance = _cfgManager.GetCVar(CCVars.AmbientOcclusionDistance);
        //var color = Color.Red;
        var target = viewport.RenderTarget;
        var lightScale = target.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var maps = _entManager.System<SharedMapSystem>();
        var lookups = _entManager.System<EntityLookupSystem>();
        var query = _entManager.System<OccluderSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var turfSystem = _entManager.System<TurfSystem>();
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        if (_aoTarget?.Texture.Size != target.Size)
        {
            _aoTarget?.Dispose();
            _aoTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-target");
        }

        if (_aoBlurBuffer?.Texture.Size != target.Size)
        {
            _aoBlurBuffer?.Dispose();
            _aoBlurBuffer = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-blur-target");
        }

        if (_aoStencilTarget?.Texture.Size != target.Size)
        {
            _aoStencilTarget?.Dispose();
            _aoStencilTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-stencil-target");
        }

        // Draw the texture data to the texture.
        args.WorldHandle.RenderInRenderTarget(_aoTarget,
            () =>
            {
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());
                var invMatrix = _aoTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);

                foreach (var entry in query.QueryAabb(mapId, worldBounds))
                {
                    DebugTools.Assert(entry.Component.Enabled);
                    var matrix = xformSystem.GetWorldMatrix(entry.Transform);
                    var localMatrix = Matrix3x2.Multiply(matrix, invMatrix);

                    worldHandle.SetTransform(localMatrix);
                    // 4 pixels
                    worldHandle.DrawRect(Box2.UnitCentered.Enlarged(distance / EyeManager.PixelsPerMeter), Color.White);
                }
            }, Color.Transparent);

        _clyde.BlurRenderTarget(viewport, _aoTarget, _aoBlurBuffer, viewport.Eye!, 14f);

        // Need to do stencilling after blur as it will nuke it.
        // Draw stencil for the grid so we don't draw in space.
        args.WorldHandle.RenderInRenderTarget(_aoStencilTarget,
            () =>
            {
                // Don't want lighting affecting it.
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());

                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
                {
                    var transform = xformSystem.GetWorldMatrix(grid.Owner);
                    var worldToTextureMatrix = Matrix3x2.Multiply(transform, invMatrix);
                    var tiles = maps.GetTilesEnumerator(grid.Owner, grid, worldBounds);
                    worldHandle.SetTransform(worldToTextureMatrix);
                    while (tiles.MoveNext(out var tileRef))
                    {
                        if (turfSystem.IsSpace(tileRef))
                            continue;

                        var bounds = lookups.GetLocalBounds(tileRef, grid.TileSize);
                        worldHandle.DrawRect(bounds, Color.White);
                    }
                }

            }, Color.Transparent);

        // Draw the stencil texture to depth buffer.
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(_aoStencilTarget!.Texture, worldBounds);

        // Draw the Blurred AO texture finally.
        worldHandle.UseShader(_proto.Index(StencilEqualDrawShader).Instance());
        worldHandle.DrawTextureRect(_aoTarget!.Texture, worldBounds, color);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }
}
